using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

// NOTE: this assembly is deliberately named "Unity.Environment.Editor.ShaderGraph". Shader Graph exposes its internals to that
// assembly name (InternalsVisibleTo), which is the only way to build real, editable .shadergraph assets from code:
// GraphData, the node classes and the shader properties are all internal. It is an editor-only tool assembly.
namespace BulletHell.EditorTools.ShaderGen
{
    /// <summary>
    /// Builds a Sprite Unlit shader graph from code. It starts from URP's own "2D Sprite Unlit" template (so the target,
    /// blocks and _MainTex property are exactly what Unity expects), strips the template's colour/alpha chain, and lets the
    /// caller wire up real nodes. Nodes are found by their menu path ("Math/Basic/Multiply") and connected by slot name.
    /// </summary>
    internal sealed class GraphBuilder
    {
        private static readonly Dictionary<string, Type> NodeTypes = BuildNodeTypeMap();

        private readonly GraphData graph;
        private readonly AbstractMaterialNode mainTexture;
        private readonly AbstractMaterialNode sampler;
        private readonly AbstractMaterialNode vertexColor;
        private readonly BlockNode baseColorBlock;
        private readonly BlockNode alphaBlock;
        private readonly Dictionary<string, AbstractShaderProperty> properties = new Dictionary<string, AbstractShaderProperty>();
        private float nextY;

        public GraphData Graph => graph;

        /// <summary>The sprite's texture sampled at the mesh UV: RGBA in "RGBA", alpha in "A".</summary>
        public AbstractMaterialNode Sprite { get; private set; }
        /// <summary>The sprite texture multiplied by the SpriteRenderer colour (vertex colour), RGBA.</summary>
        public AbstractMaterialNode Tinted { get; private set; }

        public GraphBuilder()
        {
            string templatePath = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/GraphTemplates/2D/1_2D Sprite Unlit.shadergraph";
            graph = new GraphData { messageManager = new MessageManager(), assetGuid = "" };
            MultiJson.Deserialize(graph, File.ReadAllText(Path.GetFullPath(templatePath)));
            graph.OnEnable();

            baseColorBlock = graph.fragmentContext.blocks.Select(b => b.value)
                .First(b => b.descriptor.name == "BaseColor");
            alphaBlock = graph.fragmentContext.blocks.Select(b => b.value)
                .First(b => b.descriptor.name == "Alpha");

            mainTexture = graph.GetNodes<PropertyNode>().First(n => n.property.referenceName == "_MainTex");
            sampler = graph.GetNodes<SamplerStateNode>().First();
            vertexColor = graph.GetNodes<VertexColorNode>().First();
            AbstractMaterialNode template = graph.GetNodes<SampleTexture2DNode>().First();

            // Drop everything else the template computes with (keyword branch, multiply, split, swizzle, the keyword itself).
            var keep = new HashSet<AbstractMaterialNode> { mainTexture, sampler, vertexColor, template, baseColorBlock, alphaBlock };
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>().ToList())
            {
                if (node is BlockNode block && !keep.Contains(block))
                    continue; // vertex blocks stay untouched
                if (!keep.Contains(node))
                    graph.RemoveNode(node);
            }
            foreach (ShaderKeyword keyword in graph.keywords.ToList())
                graph.RemoveGraphInput(keyword);

            Sprite = template;
            Link(mainTexture, "*", Sprite, "Texture");
            Tinted = Node("Math/Basic/Multiply");
            Link(Sprite, "RGBA", Tinted, "A");
            Link(vertexColor, "Out", Tinted, "B");
            foreach (BlockNode b in new[] { baseColorBlock, alphaBlock })
                foreach (IEdge edge in graph.GetEdges(b.GetSlotReference(0)).ToList())
                    graph.RemoveEdge(edge);
        }

        /// <summary>The shared _MainTex property node output ("Texture") and sampler state.</summary>
        public AbstractMaterialNode MainTexture => mainTexture;
        public AbstractMaterialNode Sampler => sampler;
        public AbstractMaterialNode VertexColor => vertexColor;

        // ---- nodes ---------------------------------------------------------------------------------------------

        /// <summary>Adds a node by menu path, e.g. "Math/Basic/Multiply".</summary>
        public AbstractMaterialNode Node(string menuPath)
        {
            if (!NodeTypes.TryGetValue(menuPath, out Type type))
                throw new ArgumentException($"No Shader Graph node with the menu path '{menuPath}'.");
            var node = (AbstractMaterialNode)Activator.CreateInstance(type);
            var rect = node.drawState.position;
            rect.x = 200f;
            rect.y = nextY;
            nextY += 140f;
            node.drawState = new DrawState { position = rect, expanded = true };
            graph.AddNode(node);
            return node;
        }

        /// <summary>A float constant.</summary>
        public AbstractMaterialNode Float(float value)
        {
            var node = (Vector1Node)Node("Input/Basic/Float");
            node.FindInputSlot<Vector1MaterialSlot>(Vector1Node.InputSlotXId).value = value;
            return node;
        }

        /// <summary>A Vector2 constant.</summary>
        public AbstractMaterialNode Vec2(float x, float y)
        {
            var node = (Vector2Node)Node("Input/Basic/Vector 2");
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotXId).value = x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector2Node.InputSlotYId).value = y;
            return node;
        }

        /// <summary>Sets the default value of a float input slot that has nothing connected.</summary>
        public void SetFloat(AbstractMaterialNode node, string slotName, float value)
        {
            var slot = FindSlot(node, slotName, input: true) as Vector1MaterialSlot;
            if (slot == null)
                throw new ArgumentException($"{node.name}: '{slotName}' is not a float input.");
            slot.value = value;
        }

        /// <summary>Adds an exposed float property whose reference name is exactly `reference` (the MaterialPropertyBlock key).</summary>
        public AbstractMaterialNode FloatProperty(string displayName, string reference, float defaultValue)
        {
            var property = new Vector1ShaderProperty { displayName = displayName, overrideReferenceName = reference, value = defaultValue };
            return AddProperty(property);
        }

        public AbstractMaterialNode ColorProperty(string displayName, string reference, Color defaultValue, bool hdr = false)
        {
            var property = new ColorShaderProperty { displayName = displayName, overrideReferenceName = reference, value = defaultValue };
            if (hdr)
                property.colorMode = ColorMode.HDR;
            return AddProperty(property);
        }

        public AbstractMaterialNode Vector2Property(string displayName, string reference, Vector2 defaultValue)
        {
            var property = new Vector2ShaderProperty { displayName = displayName, overrideReferenceName = reference, value = defaultValue };
            return AddProperty(property);
        }

        private AbstractMaterialNode AddProperty(AbstractShaderProperty property)
        {
            graph.AddGraphInput(property);
            properties[property.referenceName] = property;
            var node = new PropertyNode { property = property };
            var rect = node.drawState.position;
            rect.x = -200f;
            rect.y = nextY;
            nextY += 60f;
            node.drawState = new DrawState { position = rect, expanded = true };
            graph.AddNode(node);
            return node;
        }

        // ---- wiring --------------------------------------------------------------------------------------------

        /// <summary>Connects an output slot of `from` to an input slot of `to`, both by name.</summary>
        public void Link(AbstractMaterialNode from, string outputName, AbstractMaterialNode to, string inputName)
        {
            MaterialSlot output = FindSlot(from, outputName, input: false);
            MaterialSlot input = FindSlot(to, inputName, input: true);
            graph.Connect(from.GetSlotReference(output.id), to.GetSlotReference(input.id));
        }

        /// <summary>Output of a property node / single-output node (slot 0).</summary>
        public void LinkOut(AbstractMaterialNode from, AbstractMaterialNode to, string inputName)
        {
            var outputs = new List<MaterialSlot>();
            from.GetOutputSlots(outputs);
            MaterialSlot output = outputs.First();
            MaterialSlot input = FindSlot(to, inputName, input: true);
            graph.Connect(from.GetSlotReference(output.id), to.GetSlotReference(input.id));
        }

        /// <summary>Convenience: two-input math node with the given inputs connected, returns the node.</summary>
        public AbstractMaterialNode Math(string menuPath, AbstractMaterialNode a, string aOut, AbstractMaterialNode b, string bOut,
                                         string aIn = "A", string bIn = "B")
        {
            AbstractMaterialNode node = Node(menuPath);
            Link(a, aOut, node, aIn);
            Link(b, bOut, node, bIn);
            return node;
        }

        /// <summary>Feeds the final colour (rgb) and alpha into the fragment blocks.</summary>
        public void Output(AbstractMaterialNode color, string colorOut, AbstractMaterialNode alpha, string alphaOut)
        {
            MaterialSlot c = FindSlot(color, colorOut, input: false);
            MaterialSlot a = FindSlot(alpha, alphaOut, input: false);
            graph.Connect(color.GetSlotReference(c.id), baseColorBlock.GetSlotReference(0));
            graph.Connect(alpha.GetSlotReference(a.id), alphaBlock.GetSlotReference(0));
        }

        public string Save(string assetPath)
        {
            graph.ValidateGraph();
            string full = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            FileUtilities.WriteShaderGraphToDisk(assetPath, graph);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return assetPath;
        }

        // ---- helpers -------------------------------------------------------------------------------------------

        private static MaterialSlot FindSlot(AbstractMaterialNode node, string name, bool input)
        {
            var slots = new List<MaterialSlot>();
            if (input)
                node.GetInputSlots(slots);
            else
                node.GetOutputSlots(slots);
            MaterialSlot found = name == "*" ? slots.FirstOrDefault() : slots.FirstOrDefault(s => string.Equals(s.shaderOutputName, name, StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(s.displayName, name, StringComparison.OrdinalIgnoreCase));
            if (found == null)
                throw new ArgumentException($"{node.name} has no {(input ? "input" : "output")} slot '{name}'. Available: " +
                                            string.Join(", ", slots.Select(s => s.displayName)));
            return found;
        }

        private static Dictionary<string, Type> BuildNodeTypeMap()
        {
            var map = new Dictionary<string, Type>();
            foreach (Type type in typeof(AbstractMaterialNode).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(AbstractMaterialNode).IsAssignableFrom(type))
                    continue;
                var title = (TitleAttribute)Attribute.GetCustomAttribute(type, typeof(TitleAttribute));
                if (title != null)
                    map[string.Join("/", title.title)] = type;
            }
            return map;
        }
    }
}

