/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.CloudLib.Client.Models
{
    using Newtonsoft.Json;

    /// <summary>GraphQL Result for object queries</summary>
    [JsonObject("objectType")]
    public class ObjectResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty("id")]
        public int ID { get; set; }
        /// <summary>Gets or sets the nodeset identifier.</summary>
        /// <value>The nodeset identifier.</value>
        [JsonProperty("nodesetId")]
        public long NodesetID { get; set; }
        /// <summary>Gets or sets the browsename.</summary>
        /// <value>The browsename.</value>
        [JsonProperty("browseName")]
        public string Browsename { get; set; }
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [JsonProperty("value")]
        public string Value { get; set; }
        /// <summary>Gets or sets the namespace.</summary>
        /// <value>The namespace.</value>
        [JsonProperty("nameSpace")]
        public string Namespace { get; set; }
    }
}
