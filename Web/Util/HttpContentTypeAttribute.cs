using System.Reflection;
using System.Web.Mvc;

namespace Web.Util
{
    public class HttpContentTypeAttribute : ActionMethodSelectorAttribute
    {
        private readonly string _contentType;

        public HttpContentTypeAttribute(string contentType)
        {
            _contentType = contentType;
        }

        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
        {
            return controllerContext.HttpContext.Request.ContentType.StartsWith(_contentType);
        }
    }
}