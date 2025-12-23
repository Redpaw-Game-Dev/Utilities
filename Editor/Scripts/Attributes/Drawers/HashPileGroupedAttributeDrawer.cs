using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities
{
    [CustomPropertyDrawer(typeof(HashPileGroupedAttribute), true)]
    public class HashPileGroupedAttributeDrawer : HashPileDrawer
    {
        protected override void RefreshDrawer(SerializedProperty property, SerializedProperty freeListProp,
            List<int> freeSlotIndexes, SerializedProperty slotsArrayProp, Foldout elementsFoldout, SerializedProperty lastIndexProp,
            Label countLabel, object currentObj)
        {
            UpdateFreeSlotIndexes(freeListProp, freeSlotIndexes, slotsArrayProp);
            elementsFoldout.Clear();

            if (slotsArrayProp == null || !slotsArrayProp.isArray) return;
            int showedCount = 0;
            var slotsByHashDict = new Dictionary<int, Queue<SerializedProperty>>();
            for (int i = 0; i < slotsArrayProp.arraySize && i < lastIndexProp.intValue; i++)
            {
                var element = slotsArrayProp.GetArrayElementAtIndex(i);

                if (freeSlotIndexes.Contains(i) || IsNullElement(element)) continue;

                var hash = element.FindPropertyRelative("HashCode").intValue;
                if (!slotsByHashDict.ContainsKey(hash)) slotsByHashDict.Add(hash, new Queue<SerializedProperty>());
                slotsByHashDict[hash].Enqueue(element);
            }

            var hashCounter = 0;
            foreach (var slotsByHashPair in slotsByHashDict)
            {
                var hashRoot = new VisualElement() { name = $"hashRoot_{slotsByHashPair.Key}" };
                elementsFoldout.Add(hashRoot);

                var hashFoldout = new Foldout()
                {
                    text = $"Element {hashCounter++}",
                    value = false
                };
                hashRoot.Add(hashFoldout);

                var hashCountLabel = new Label();
                hashCountLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleRight);
                hashCountLabel.style.position = new StyleEnum<Position>(Position.Absolute);
                hashCountLabel.style.alignSelf = new StyleEnum<Align>(Align.FlexEnd);
                hashCountLabel.style.flexShrink = new StyleFloat(0f);
                hashCountLabel.style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
                hashCountLabel.style.marginTop = new StyleLength(2f);
                hashRoot.Add(hashCountLabel);

                var hashScrollView = new ScrollView();
                hashScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                hashScrollView.style.marginLeft = ScrollViewMarginLeft;
                hashScrollView.contentContainer.style.marginLeft = ScrollViewContentMarginLeft;
                hashScrollView.style.backgroundColor = UnityBgStyleColor;
                hashScrollView.AddToClassList("unity-collection-view--with-border");
                hashFoldout.Add(hashScrollView);

                int hashSlotsShowedCount = 0;
                while (slotsByHashPair.Value.Count > 0)
                {
                    var slotProp = slotsByHashPair.Value.Dequeue();
                    var row = BuildPileVisualElement(slotProp, showedCount++ % 2 == 0, currentObj, property, freeListProp,
                        freeSlotIndexes, slotsArrayProp, elementsFoldout, lastIndexProp, countLabel);
                    hashSlotsShowedCount++;
                    hashScrollView.Add(row);
                }

                hashCountLabel.text = $"Count: {hashSlotsShowedCount}";
            }

            countLabel.text = $"Count: {showedCount}";
        }
    }
}