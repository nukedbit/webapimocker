using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using Moq;

namespace NukedBit.WebApiMocker
{
    public interface IControllerMocker<out T> where T : ApiController
    {
        IControllerMocker<T> Configure(Action<HttpConfiguration> httpConfigAction);

        IControllerMocker<T> RequestContent(HttpContent httpContent);

        IControllerMocker<T> Filters(IEnumerable<FilterInfo> filters);

        IControllerMocker<T> AutoMapFilters();

        IControllerMocker<T> Request(Uri requestUri, HttpMethod method);

        IControllerMocker<T> UrlHelperLink(Uri uri);

        IControllerMocker<T> UrlHelperCustom(Action<Mock<UrlHelper>> urlHelperMockAction);

        IControllerMocker<T> Build();
        
        HttpResponseMessage Execute();
    }
}
