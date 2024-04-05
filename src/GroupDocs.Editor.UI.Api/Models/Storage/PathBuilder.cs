using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupDocs.Editor.UI.Api.Models.Storage
{
    public class PathBuilder
    {
        private PathBuilder(Guid documentCode, string[]? postfixKeys = null)
        {
            DocumentCode = documentCode;
            PostfixKeys = postfixKeys ?? Array.Empty<string>();
        }

        public static PathBuilder New(Guid documentCode, string[]? postfixKeys = null)
        {
            return new PathBuilder(documentCode, postfixKeys);
        }

        public Guid DocumentCode { get; }

        public string[] PostfixKeys { get;  protected set; }

        public string[] FullPathStrings
        {
            get
            {
                var pathStrings = new[] {DocumentCode.ToString()}.Union(PostfixKeys);
                return pathStrings.ToArray();
            }
        }

        public PathBuilder AppendKey(string key)
        {
            PostfixKeys = PostfixKeys.Append(key).ToArray();
            return this;
        }

        public string ToUriPath()
        {
            return string.Join('/', FullPathStrings);
        }
        public string ToAwsPath()
        {
            return ToUriPath();
        }

        public string ToPath()
        {
            return Path.Combine(FullPathStrings);
        }

        public string ToAzurePath()
        {
            return ToUriPath();
        }
    }
}
