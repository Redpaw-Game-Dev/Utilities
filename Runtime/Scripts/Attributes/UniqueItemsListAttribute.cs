using UnityEngine;

namespace LazyRedpaw.Utilities
{
    public class UniqueItemsListAttribute : PropertyAttribute
    {
        private string _propertyName;
        private string[] _additionalNames;

        public UniqueItemsListAttribute(string propertyName, string[] additionalNames = null)
        {
            _propertyName = propertyName;
            _additionalNames = additionalNames;
        }

        public string PropertyName => _propertyName;
        public string[] AdditionalNames => _additionalNames;
    }
}