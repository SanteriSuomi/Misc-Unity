using System.Runtime.Serialization;
using UnityEngine;

namespace Essentials.Saving
{
    public class Vector2Surrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector2 vector2 = (Vector2)obj;
            info.AddValue("x", vector2.x);
            info.AddValue("y", vector2.y);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Vector2 vector2 = (Vector2)obj;
            vector2.x = (float)info.GetValue("x", typeof(float));
            vector2.y = (float)info.GetValue("y", typeof(float));
            return vector2;
        }
    }
}