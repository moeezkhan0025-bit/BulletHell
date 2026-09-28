using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// A vertical list of MenuRows under one container. Rows are created on demand and reused: call Begin, Add for each
    /// entry, then End (hides the unused rows). Rebuilding a list therefore doesn't allocate new objects.
    /// </summary>
    public sealed class UIList : MonoBehaviour
    {
        [SerializeField] private MenuRow rowPrefab;
        [SerializeField] private RectTransform content;

        private readonly List<MenuRow> rows = new List<MenuRow>();
        private int used;

        public int Count => used;

        public void Begin() => used = 0;

        public MenuRow Add(string text, Action select, Action cancel = null, bool dim = false)
        {
            if (used == rows.Count)
                rows.Add(Instantiate(rowPrefab, content));

            MenuRow row = rows[used++];
            row.gameObject.SetActive(true);
            row.transform.SetSiblingIndex(used - 1);
            row.Setup(text, select, cancel, dim);
            return row;
        }

        public void End()
        {
            for (int i = used; i < rows.Count; i++)
                rows[i].gameObject.SetActive(false);
        }

        /// <summary>Focuses a row (clamped to the list). Does nothing for an empty list.</summary>
        public void Focus(int index)
        {
            if (used == 0)
                return;
            UIFocusGuard.Focus(rows[Mathf.Clamp(index, 0, used - 1)].gameObject);
        }
    }
}
