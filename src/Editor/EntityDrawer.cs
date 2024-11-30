using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace npg.tomatoecs.Editor
{
    [CustomEditor(typeof(EntityView), true)]
    public class EntityDrawer : UnityEditor.Editor
    {
        private const string Suffix = "Component";

        private static bool _isEntityMode;
        private static Context _context;
        private static List<Type> _components;
        private static uint _currentEntityId;
        private static Entity _currentEntity;
        private static MethodInfo _hasComponentMethod;
        private static MethodInfo _getComponentMethod;
        private static MethodInfo _setComponentMethod;
        private static MethodInfo _removeComponentMethod;
        private static MethodInfo _addComponentMethod;
        private static GUIStyle _boldStyle;
        private static bool _hashSetFoldout;
        private static GUIStyle _coloredBoxStyle;
        private static bool _isValueSelected;
        private static Type _selectedValue;

        private void OnEnable()
        {
            _hasComponentMethod = typeof(Entity).GetMethod("HasComponent", new Type[] { });
            _addComponentMethod =
                typeof(Entity).GetMethod("AddComponentFromEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            _removeComponentMethod = typeof(Entity).GetMethod("RemoveComponent", new Type[] { });
            _getComponentMethod =
                typeof(Entity).GetMethod("GetComponentForEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            _setComponentMethod =
                typeof(Entity).GetMethod("SetComponent", BindingFlags.NonPublic | BindingFlags.Instance);
            var worlContext = FindAnyObjectByType<WorldContextObject>();
            if (worlContext == null)
            {
                return;
            }

            _context = worlContext.Context;
            _components = worlContext.Components;
            _boldStyle = new GUIStyle(EditorStyles.boldLabel);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_coloredBoxStyle == null)
            {
                _coloredBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeTex(1, 1, Color.grey) }
                };
            }

            var entityView = (EntityView)target;

            _isEntityMode = EditorGUILayout.Toggle("Entity mode", _isEntityMode);

            if (_isEntityMode)
            {
                GUILayout.BeginHorizontal();
                _currentEntityId = entityView.EntityId;


                _currentEntity = _context.GetEntity(_currentEntityId);

                GUILayout.EndHorizontal();
                GUILayout.BeginVertical();
                if (_currentEntity.IsActive)
                {
                    DrawEntity(_currentEntity);
                }

                GUILayout.EndVertical();
            }
            else

            {
                base.OnInspectorGUI();

                serializedObject.ApplyModifiedProperties();
                if (Application.isPlaying == false)
                {
                    this.Repaint();
                }
            }
        }

        public static void DrawEntity(Entity entity)
        {
            _boldStyle.normal.textColor = Color.red;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", "Entity ID", _boldStyle, GUILayout.Width(100));
            EditorGUILayout.LabelField("", entity.Id.ToString(), _boldStyle, GUILayout.Width(50));
            var addComponent = GUILayout.Button("+", GUILayout.Width(30));
            GUILayout.EndHorizontal();

            if (addComponent)
            {
                ShowAddComponentMenu(entity);
            }

            if (_isValueSelected)
            {
                var genericAddMethod = _addComponentMethod.MakeGenericMethod(_selectedValue);
                genericAddMethod.Invoke(entity, new object[] { });
                _isValueSelected = false;
            }


            foreach (var componentType in _components)
            {
                var genericHasComponentMethod = _hasComponentMethod.MakeGenericMethod(componentType);
                var hasComponent = (bool)genericHasComponentMethod.Invoke(entity, new object[] { });

                if (hasComponent)
                {
                    var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                    GUILayout.BeginVertical(_coloredBoxStyle);

                    GUILayout.BeginHorizontal();
                    DrawComponentName(componentType.Name);
                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        var genericRemoveMethod = _removeComponentMethod.MakeGenericMethod(componentType);
                        genericRemoveMethod.Invoke(entity, new object[] { });
                        continue;
                    }

                    GUILayout.EndHorizontal();

                    var genericGetComponentMethod = _getComponentMethod.MakeGenericMethod(componentType);
                    var componentRef =
                        genericGetComponentMethod.Invoke(entity, new object[] { });


                    for (var i = 0; i < fields.Length; i++)
                    {
                        var fieldInfo = fields[i];
                        var value = fieldInfo.GetValue(componentRef);
                        DrawField(fieldInfo, componentRef, value);

                        if (GUI.changed)
                        {
                            var genericSetComponentMethod = _setComponentMethod.MakeGenericMethod(componentType);
                            genericSetComponentMethod.Invoke(entity, new[] { componentRef });
                        }
                    }

                    GUILayout.EndVertical();
                }
            }
        }

        private static void ShowAddComponentMenu(Entity entity)
        {
            GenericMenu menu = new GenericMenu();

            foreach (var type in _components)
            {
                var genericHasComponentMethod = _hasComponentMethod.MakeGenericMethod(type);
                var hasComponent = (bool)genericHasComponentMethod.Invoke(entity, new object[] { });


                if (hasComponent)
                {
                    continue;
                }

                menu.AddItem(new GUIContent(RemoveSuffix(type.Name)), false, () => OnOptionSelected(type));
            }

            menu.ShowAsContext();
        }

        private static void OnOptionSelected(Type kvpValue)
        {
            _isValueSelected = true;
            _selectedValue = kvpValue;
        }

        private static void DrawComponentName(string componentName)
        {
            var textColor = new Color(253f, 115f, 0f, 255f);
            _boldStyle.normal.textColor = textColor;
            _boldStyle.hover.textColor = textColor;

            // Rect rect = GUILayoutUtility.GetLastRect();
            // Rect labelRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 20); // Отступы по 10 пикселей
            GUILayout.Label($"{RemoveSuffix(componentName)}", _boldStyle);
        }

        private static string RemoveSuffix(string input)
        {
            if (input.EndsWith(Suffix))
            {
                return input.Substring(0, input.Length - Suffix.Length);
            }

            return input;
        }

        private static void DrawField(FieldInfo fieldInfo, object component, object value)
        {
            var name = fieldInfo.Name;
            if (value is int intValue)
            {
                var editorValue = EditorGUILayout.IntField(name, intValue);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is string stringValue)
            {
                var editorValue = EditorGUILayout.TextField(name, stringValue);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is float floatValue)
            {
                var editorValue = EditorGUILayout.FloatField(name, floatValue);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is uint uintValue)
            {
                var tempInt = Convert.ToInt32(uintValue);
                tempInt = EditorGUILayout.IntField(name, tempInt);
                fieldInfo.SetValue(component, (uint)tempInt);
            }
            else if (value.GetType().IsEnum)
            {
                var editorValue = EditorGUILayout.EnumPopup(name, (Enum)value);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value.GetType().IsSubclassOf(typeof(Object)))
            {
                var editorValue = EditorGUILayout.ObjectField(name, (Object)value, fieldInfo.FieldType);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is Vector3 vector3Value)
            {
                var editorValue = EditorGUILayout.Vector3Field(name, vector3Value);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is Vector2 vector2Value)
            {
                var editorValue = EditorGUILayout.Vector2Field(name, vector2Value);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is Vector3Int vector3IntValue)
            {
                var editorValue = EditorGUILayout.Vector3IntField(name, vector3IntValue);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is Vector2Int vector2IntValue)
            {
                var editorValue = EditorGUILayout.Vector2IntField(name, vector2IntValue);
                fieldInfo.SetValue(component, editorValue);
            }
            else if (value is IEnumerable iEnumerable)
            {
                DrawCollection(fieldInfo, component, value);
                fieldInfo.SetValue(component, value);
            }
            else
            {
                EditorGUILayout.LabelField(fieldInfo.Name, value.ToString());
            }
        }

        private static void DrawCollection(FieldInfo fieldInfo, object component, object obj)
        {
            Type type = obj.GetType();
            var xx = type.GetGenericTypeDefinition();
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            {
                Type elementType = type.GetGenericArguments()[0];
                var method = type.GetMethod("GetEnumerator");
                var enumerator = method.Invoke(obj, null) as IEnumerator;

                var elements = new List<object>();
                int count = 0;
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    elements.Add(enumerator.Current);
                    count++;
                }

                GUILayout.BeginHorizontal();
                _hashSetFoldout = EditorGUILayout.Foldout(_hashSetFoldout,
                    $"{fieldInfo.Name} - {type.Name}<{elementType.Name}> (Count: {count})", true);
                var addElement = GUILayout.Button("+", GUILayout.Width(30));
                GUILayout.EndHorizontal();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                if (_hashSetFoldout)
                {
                    if (typeof(IList).IsAssignableFrom(type)) // Если это List<T>
                    {
                        IList list = (IList)obj;


                        if (addElement)
                        {
                            var addedItem = Activator.CreateInstance(elementType);
                            list.Add(addedItem);
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            object item = list[i];

                            GUILayout.BeginHorizontal();
                            object newItem = DrawElementField(fieldInfo.Name, item);

                            if (GUILayout.Button("-", GUILayout.Width(30)))
                            {
                                list.Remove(item);
                                continue;
                            }

                            GUILayout.EndHorizontal();

                            if (!Equals(newItem, item))
                            {
                                list[i] = newItem;
                            }
                        }
                    }
                    else if (type.GetInterfaces().Any(i =>
                                 i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>)))
                    {
                        MethodInfo addMethod = type.GetMethod("Add");
                        MethodInfo removeMethod = type.GetMethod("Remove");

                        foreach (var item in elements)
                        {
                            if (addElement)
                            {
                                var addedItem = Activator.CreateInstance(elementType);
                                addMethod.Invoke(obj, new[] { addedItem });
                            }

                            GUILayout.BeginHorizontal();
                            object newItem = DrawElementField(fieldInfo.Name, item);

                            if (GUILayout.Button("-", GUILayout.Width(30)))
                            {
                                removeMethod.Invoke(obj, new[] { item });
                                continue;
                            }

                            GUILayout.EndHorizontal();
                            if (!Equals(newItem, item))
                            {
                                removeMethod.Invoke(obj, new[] { item });
                                addMethod.Invoke(obj, new[] { newItem });
                            }
                        }
                    }
                }
            }
        }

        private static object DrawElementField(string name, object item)
        {
            if (item is uint uintValue)
            {
                var tempInt = Convert.ToInt32(uintValue);
                var intValue = EditorGUILayout.IntField(name, tempInt);
                uintValue = Convert.ToUInt32(intValue);
                return uintValue;
            }

            if (item.GetType().IsSubclassOf(typeof(Object)))
            {
                return EditorGUILayout.ObjectField(name, (Object)item, item.GetType());
            }

            EditorGUILayout.LabelField(name, item.ToString());
            return item;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = tex.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = col;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}