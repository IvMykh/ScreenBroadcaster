using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenBroadcaster.Common
{
    public class CommandParameter
    {
        public class Property
        {
            public string Name   { get; set; }
            public object Value { get; set; }
        }

        public List<Property> _properties;
        
        public CommandParameter()
        {
            _properties = new List<Property>();
        }

        public object this[string propertyName]
        {
            get
            {
                var foundProp = (_properties.FirstOrDefault(
                    prop => string.CompareOrdinal(prop.Name, propertyName) == 0));

                return (foundProp != null) ? foundProp.Value : null;
            }
            set
            {
                _properties.Add(
                    new Property 
                        { 
                            Name = propertyName, 
                            Value = value 
                        });
            }
        }
    }
}
