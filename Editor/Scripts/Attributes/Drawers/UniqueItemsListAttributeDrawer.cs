using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities.Editor
{
    [CustomPropertyDrawer(typeof(UniqueItemsListAttribute))]
    public class UniqueItemsListAttributeDrawer : PropertyDrawer
    {
        private readonly string ListViewStyle = "list-view";
        private readonly string ScrolViewStyle = "scroll-view";
        private readonly string SizeFieldStyle = "size-field";
        private readonly string FoldoutStyle = "foldout";
        private readonly string EmptyLabelStyle = "empty-label";
        private readonly string RemoveItemButtonStyle = "remove-item-button";
        private readonly string ScrolViewItemStyle = "scroll-view-item";

        private readonly string NoElementsString = "No elements to add";
        private readonly string ListElementName = "List Element";
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var attr = (UniqueItemsListAttribute)attribute;
            root.Add(CreateUniqueItemsList(property.FindPropertyRelative(attr.PropertyName)));
            if (attr.AdditionalNames != null)
            {
                for (int i = 0; i < attr.AdditionalNames.Length; i++)
                {
                    root.Add(CreateUniqueItemsList(property.FindPropertyRelative(attr.AdditionalNames[i])));
                }
            }
            return root;
        }

        private VisualElement CreateUniqueItemsList(SerializedProperty property)
        {
            var fieldType = property.GetFieldType();

            var isArray = fieldType.IsArray;
            var isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);

            if (!isArray && !isList) return null;

            var requiredType = isList ? fieldType.GetGenericArguments()[0] : fieldType.GetElementType();

            var allTypes = requiredType.GetSubclasses();
            var availableTypeNames = new List<string>();
            
            var typeMap = new Dictionary<string, Type>(allTypes.Count);
            for (int i = 0; i < allTypes.Count; i++)
            {
                var type = allTypes[i];
                availableTypeNames.Add(type.Name);
                typeMap.Add(type.Name, type);
            }

            for (int j = 0; j < property.arraySize; j++)
            {
                SerializedProperty elementProp = property.GetArrayElementAtIndex(j);
                var type = elementProp.managedReferenceValue.GetType();
                availableTypeNames.Remove(type.Name);
            }

            var list = new ListView
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                reorderable = true,
                showAddRemoveFooter = true,
                showFoldoutHeader = true,
                headerTitle = property.displayName,
            };
            list.style.flexGrow = new StyleFloat(1f);
            var listFoldoutContent = list.Q<ScrollView>();
            var listContentPadding = new Length(2.4f, LengthUnit.Pixel);
            listFoldoutContent.SetPadding(top: listContentPadding, bottom: listContentPadding);
            listFoldoutContent.SetBorderWidth(1f);
            listFoldoutContent.SetBorderRadius(new Length(3f, LengthUnit.Pixel));

            var sizeField = list.Q<TextField>("unity-list-view__size-field");
            sizeField.isReadOnly = true;

            list.makeItem = () =>
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var reorderableHint = new Label("☰")
                {
                    style =
                    {
                        width = new StyleLength(16f),
                        unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.UpperCenter),
                        opacity = new StyleFloat(0.3f),
                        paddingTop = new StyleLength(3f)
                    }
                };

                var propField = new PropertyField()
                {
                    name = ListElementName,
                    style =
                    {
                        flexGrow = new StyleFloat(1f),
                        paddingLeft = new StyleLength(10f),
                        paddingRight = new StyleLength(3f)
                    }
                };
                
                row.Add(reorderableHint);
                row.Add(propField);
                
                return row;
            };
            list.bindItem = (visualElement, i) =>
            {
                var propField = visualElement.Q<PropertyField>(ListElementName);
                var elementProp = property.GetArrayElementAtIndex(i);
                propField.label = elementProp.managedReferenceValue.GetType().Name;
                propField.BindProperty(elementProp);
            };
            
            list.BindProperty(property);

            list.schedule.Execute(ReplaceBuiltInAddButton);
            
            return list;
            
            void ReplaceBuiltInAddButton()
            {
                var old = list.Query<Button>("unity-list-view__add-button").ToList()[^1];
                if (old != null)
                {
                    var parent = old.parent;
                    old.RemoveFromHierarchy();
                    var addButton = new Button { name = "unity-list-view__add-button", text = "+" };
                    addButton.AddToClassList("unity-list-view__add-button");
                    addButton.clicked += AddButtonClickedCallback;
                    parent.Insert(0, addButton);
                }
            }
            
            void AddButtonClickedCallback()
            {
                availableTypeNames.Clear();
                for (int i = 0; i < allTypes.Count; i++)
                {
                    bool isExisting = false;
                    var type = allTypes[i];
                    for (int j = 0; j < property.arraySize; j++)
                    {
                        var elementProp = property.GetArrayElementAtIndex(j);
                        var otherType = elementProp.managedReferenceValue.GetType();
                        if (type == otherType)
                        {
                            isExisting = true;
                            break;
                        }
                    }
                    if(!isExisting) availableTypeNames.Add(type.Name);
                }
                
                var addMenu = new GenericMenu();
                if (property.arraySize < allTypes.Count)
                {
                    for (int i = 0; i < availableTypeNames.Count; i++)
                    {
                        addMenu.AddItem(new GUIContent(availableTypeNames[i]), false, AddItemSelectedCallback, availableTypeNames[i]);
                    
                        void AddItemSelectedCallback(object typeNameObj)
                        {
                            var typeName = (string)typeNameObj;
                        
                            var type = typeMap[typeName];
                            property.InsertArrayElementAtIndex(property.arraySize);
                            var newElementProp = property.GetArrayElementAtIndex(property.arraySize - 1);
                            newElementProp.managedReferenceValue = Activator.CreateInstance(type);
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                            AssetDatabase.Refresh();
                        }
                    }
                }
                else
                {
                    addMenu.AddDisabledItem(new GUIContent("No elements to add"));
                }
                addMenu.ShowAsContext();
            }
        }
    }
}