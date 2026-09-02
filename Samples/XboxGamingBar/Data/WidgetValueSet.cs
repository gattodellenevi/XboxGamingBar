using Shared.Data;
using System.Collections;
using System.Linq;
using Windows.Foundation.Collections;

namespace XboxGamingBar.Data
{
    internal class WidgetValueSet : SharedValueSet
    {
        public ValueSet ValueSet { get; }

        public WidgetValueSet()
        {
            ValueSet = new ValueSet();
        }

        public WidgetValueSet(ValueSet inValueSet)
        {
            ValueSet = inValueSet ?? new ValueSet();
        }

        public override object this[string key]
        {
            get { return ValueSet != null ? ValueSet[key] : null; }
        }

        public override void Add(string key, object value)
        {
            ValueSet?.Add(key, value);
        }

        public override bool TryGetValue(string key, out object value)
        {
            if (ValueSet != null)
            {
                return ValueSet.TryGetValue(key, out value);
            }
            value = null;
            return false;
        }

        public override string ToDebugString()
        {
            return "{}";
        }

        public override IEnumerator GetEnumerator()
        {
            return ValueSet.AsEnumerable().GetEnumerator();
        }
    }
}
