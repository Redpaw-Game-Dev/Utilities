using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities
{
    [CustomPropertyDrawer(typeof(HashPile<>), true)]
    public class HashPileDrawer : PropertyDrawer
    {
        protected const string UnityCollectionViewWithBorder = "unity-collection-view--with-border";
        
        protected static readonly StyleLength ScrollViewContentMarginLeft = new(15f);
        protected static readonly StyleLength ScrollViewMarginLeft = new(-15f);
        protected static readonly StyleLength FoldoutContentPaddingLeft = new(15f);
        protected static readonly StyleLength FoldoutContentSpaceRight = new(5f);
        protected static readonly StyleLength FoldoutContentMarginLeft = new(-5f);
        protected static readonly StyleLength SetElemVerticalPadding = new(2f);
        protected static readonly StyleLength SetElemHorizontalPadding = new(2f);
        protected static readonly StyleColor UnityBgStyleColor = new(new Color(0.27f, 0.27f, 0.27f));
        protected static readonly StyleColor UnityBgStyleColor2 = new(new Color(0.22f, 0.22f, 0.22f));

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var slotsArrayProp = property.FindPropertyRelative("_slots");
            var lastIndexProp = property.FindPropertyRelative("_lastIndex");
            var freeListProp = property.FindPropertyRelative("_freeList");
            var freeSlotIndexes = new List<int>();

            var root = new VisualElement();
            
            var rootFoldout = new Foldout() { text = property.displayName };
            root.Add(rootFoldout);

            var countLabel = new Label();
            countLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleRight);
            countLabel.style.position = new StyleEnum<Position>(Position.Absolute);
            countLabel.style.alignSelf = new StyleEnum<Align>(Align.FlexEnd);
            countLabel.style.flexShrink = new StyleFloat(0f);
            countLabel.style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
            countLabel.style.marginTop = new StyleLength(2f);
            root.Add(countLabel);

            var scrollView = new ScrollView();
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.style.marginLeft = ScrollViewMarginLeft;
            scrollView.contentContainer.style.marginLeft = ScrollViewContentMarginLeft;
            scrollView.style.backgroundColor = UnityBgStyleColor;
            scrollView.AddToClassList(UnityCollectionViewWithBorder);
            rootFoldout.Add(scrollView);

            var addNewElementFoldout = new Foldout() { text = "Add New Element" };
            addNewElementFoldout.contentContainer.style.marginRight = FoldoutContentSpaceRight;
            addNewElementFoldout.contentContainer.style.marginLeft = FoldoutContentMarginLeft;
            addNewElementFoldout.contentContainer.style.paddingRight = FoldoutContentSpaceRight;
            addNewElementFoldout.contentContainer.style.paddingLeft = FoldoutContentPaddingLeft;
            addNewElementFoldout.contentContainer.style.backgroundColor = UnityBgStyleColor2;
            addNewElementFoldout.contentContainer.AddToClassList(UnityCollectionViewWithBorder);
            scrollView.contentContainer.Add(addNewElementFoldout);

            var elementsRoot = new VisualElement();
            scrollView.contentContainer.Add(elementsRoot);

            var elementsFoldout = new Foldout() { text = "Elements" };
            elementsFoldout.contentContainer.style.marginRight = FoldoutContentSpaceRight;
            elementsFoldout.contentContainer.style.marginLeft = FoldoutContentMarginLeft;
            elementsFoldout.contentContainer.style.paddingRight = FoldoutContentSpaceRight;
            elementsFoldout.contentContainer.style.paddingLeft = FoldoutContentPaddingLeft;
            elementsFoldout.contentContainer.style.backgroundColor = UnityBgStyleColor2;
            elementsFoldout.contentContainer.AddToClassList(UnityCollectionViewWithBorder);
            elementsRoot.Add(elementsFoldout);

            var fieldType = fieldInfo.FieldType;
            var elementType = fieldType.GetGenericArguments()[0];
            var wrapper = ScriptableObject.CreateInstance<AddHashPileElementWrapper>();
            var so = new SerializedObject(wrapper);
            var valueProp = so.FindProperty(nameof(wrapper.newElement));

            if (elementType == typeof(int) ||
                elementType == typeof(uint) ||
                elementType == typeof(short) ||
                elementType == typeof(ushort) ||
                elementType == typeof(byte)) CreateValueField(new IntegerField(), wrapper, valueProp, addNewElementFoldout);
            else if (elementType == typeof(float)) CreateValueField(new FloatField(), wrapper, valueProp, addNewElementFoldout);
            else if (elementType == typeof(double)) CreateValueField(new DoubleField(), wrapper, valueProp, addNewElementFoldout);
            else if (elementType == typeof(string)) CreateValueField(new TextField(), wrapper, valueProp, addNewElementFoldout);
            else
            {
                wrapper.newElement = Activator.CreateInstance(elementType);
                var field = new PropertyField(valueProp, valueProp.displayName);
                field.Bind(so);
                addNewElementFoldout.Add(field);
            }

            var currentObj = fieldInfo.GetValue(property.serializedObject.targetObject);
            var addButton = new Button(() =>
            {
                OnAddButtonClickHandle(currentObj, property, wrapper);
                RefreshDrawer(property, freeListProp, freeSlotIndexes, slotsArrayProp, elementsFoldout, lastIndexProp, countLabel, currentObj);
            })
            {
                text = "Add"
            };
            addNewElementFoldout.Add(addButton);
            
            rootFoldout.RegisterCallback<SerializedPropertyChangeEvent>(_ => RefreshDrawer(property, freeListProp, freeSlotIndexes, slotsArrayProp, elementsFoldout, lastIndexProp, countLabel, currentObj));

            RefreshDrawer(property, freeListProp, freeSlotIndexes, slotsArrayProp, elementsFoldout, lastIndexProp, countLabel, currentObj);
            return root;
        }

        protected virtual void RefreshDrawer(SerializedProperty property, SerializedProperty freeListProp, List<int> freeSlotIndexes,
            SerializedProperty slotsArrayProp, Foldout elementsFoldout, SerializedProperty lastIndexProp,
            Label countLabel, object currentObj)
        {
            UpdateFreeSlotIndexes(freeListProp, freeSlotIndexes, slotsArrayProp);
            elementsFoldout.Clear();
        
            if (slotsArrayProp == null || !slotsArrayProp.isArray) return;
            int showedCount = 0;
            for (int i = 0; i < slotsArrayProp.arraySize && i < lastIndexProp.intValue; i++)
            {
                var element = slotsArrayProp.GetArrayElementAtIndex(i);
        
                if (freeSlotIndexes.Contains(i) || IsNullElement(element)) continue;
        
                var row = BuildPileVisualElement(element, showedCount++ % 2 == 0, currentObj, property, freeListProp,
                    freeSlotIndexes, slotsArrayProp, elementsFoldout, lastIndexProp, countLabel);
                elementsFoldout.Add(row);
            }
                
            countLabel.text = $"Count: {showedCount}";
        }

        protected void OnAddButtonClickHandle(object currentObj, SerializedProperty property, AddHashPileElementWrapper wrapper)
        {
            var addMethod = currentObj.GetType().GetMethod("Add",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(object) }, null);
            addMethod.Invoke(currentObj, new object[] { wrapper.newElement });
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        protected VisualElement BuildPileVisualElement(SerializedProperty slotProp, bool isEven, object currentObj,
            SerializedProperty property, SerializedProperty freeListProp, List<int> freeSlotIndexes,
            SerializedProperty slotsArrayProp, Foldout elementsFoldout, SerializedProperty lastIndexProp,
            Label countLabel)
        {
            var root = new VisualElement();
            root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            if (isEven) root.style.backgroundColor = UnityBgStyleColor;
            root.style.paddingLeft = SetElemHorizontalPadding;
            root.style.paddingRight = SetElemHorizontalPadding;
            root.style.paddingTop = SetElemVerticalPadding;
            root.style.paddingBottom = SetElemVerticalPadding;

            var valueProp = slotProp.FindPropertyRelative("Value");
            var elementType = GetFieldType(valueProp);

            if (elementType == typeof(int) ||
                elementType == typeof(uint) ||
                elementType == typeof(short) ||
                elementType == typeof(ushort) ||
                elementType == typeof(byte)) CreateElementValueField(new IntegerField(), slotProp, valueProp, root);
            else if (elementType == typeof(float)) CreateElementValueField(new FloatField(), slotProp, valueProp, root);
            else if (elementType == typeof(double)) CreateElementValueField(new DoubleField(), slotProp, valueProp, root);
            else if (elementType == typeof(string)) CreateElementValueField(new TextField(), slotProp, valueProp, root);
            else
            {
                var field = new PropertyField(valueProp, slotProp.displayName);
                field.style.flexGrow = new StyleFloat(1f);
                field.Bind(valueProp.serializedObject);
                root.Add(field);
            }

            var removeButton = new Button(() =>
            {
                var instance = valueProp.boxedValue;
                var removeMethod = currentObj.GetType().GetMethod("Remove",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(object) }, null);
                var resultObj = removeMethod.Invoke(currentObj, new object[] { instance });
                var isRemoved = resultObj is bool b && b;
                if (isRemoved)
                {
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                    RefreshDrawer(property, freeListProp, freeSlotIndexes, slotsArrayProp, elementsFoldout, lastIndexProp, countLabel, currentObj);
                }
            })
            {
                text = "x"
            };
            root.Add(removeButton);

            return root;
        }

        protected void CreateElementValueField<T>(BaseField<T> valueField, SerializedProperty slotProp,
            SerializedProperty valueProp, VisualElement root)
        {
            valueField.label = slotProp.displayName;
            valueField.style.flexGrow = new StyleFloat(1f);
            valueField.Bind(valueProp.serializedObject);
            valueField.BindProperty(valueProp);
            root.Add(valueField);
        }

        protected void UpdateFreeSlotIndexes(SerializedProperty freeListProp, List<int> freeSlotIndexes,
            SerializedProperty slotsArrayProp)
        {
            int freeListTmp = freeListProp.intValue;
            freeSlotIndexes.Clear();
            while (freeListTmp >= 0)
            {
                int index = freeListTmp;
                freeSlotIndexes.Add(index);
                var slotProp = slotsArrayProp.GetArrayElementAtIndex(index);
                var slotNextProp = slotProp.FindPropertyRelative("Next");
                freeListTmp = slotNextProp.intValue - 1;
            }
        }

        protected void CreateValueField<T>(BaseField<T> valueField, AddHashPileElementWrapper wrapper,
            SerializedProperty valueProp, Foldout addNewElementFoldout)
        {
            wrapper.newElement = default(T);
            valueField.label = valueProp.displayName;
            addNewElementFoldout.Add(valueField);

            valueField.RegisterValueChangedCallback(evt => wrapper.newElement = evt.newValue);
        }

        protected bool IsNullElement(SerializedProperty element)
        {
            var valueProp = element.FindPropertyRelative("Value");

            if (valueProp.propertyType == SerializedPropertyType.ManagedReference &&
                valueProp.managedReferenceValue == null) return true;

            if (valueProp.propertyType == SerializedPropertyType.ObjectReference &&
                valueProp.objectReferenceValue == null) return true;

            return false;
        }
        
        protected Type GetFieldType(SerializedProperty property)
        {
            Type parentType = property.serializedObject.targetObject.GetType();
            string path = property.propertyPath.Replace(".Array.data[", "[");

            object currentObject = property.serializedObject.targetObject;

            foreach (var element in path.Split('.'))
            {
                if (element.Contains("["))
                {
                    string fieldName = element.Substring(0, element.IndexOf("["));
                    int index = int.Parse(element.Substring(element.IndexOf("["))
                        .Replace("[", "").Replace("]", ""));

                    FieldInfo fi = GetFieldInfo(parentType, fieldName);
                    parentType = fi.FieldType.IsArray ? fi.FieldType.GetElementType()
                        : fi.FieldType.GetGenericArguments()[0];

                    currentObject = GetIndexedValue(currentObject, fieldName, index);
                }
                else
                {
                    FieldInfo fi = GetFieldInfo(parentType, element);
                    parentType = fi.FieldType;
                    currentObject = fi.GetValue(currentObject);
                }
            }

            return parentType;
        }

        protected FieldInfo GetFieldInfo(Type type, string name)
        {
            return type.GetField(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        protected object GetIndexedValue(object source, string name, int index)
        {
            var enumerable = GetFieldInfo(source.GetType(), name).GetValue(source) as IEnumerable;
            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++) enumerator.MoveNext();
            return enumerator.Current;
        }
    }
}