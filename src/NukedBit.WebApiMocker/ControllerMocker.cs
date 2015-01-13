using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using System.Web.Http.Services;
using Moq;

namespace NukedBit.WebApiMocker
{
    /// <summary>
    /// Mock your controller
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ControllerMocker<T> : IControllerMocker<T> where T : ApiController
    {
        private readonly T _controller;

        private Action<HttpConfiguration> _httpConfigAction;
        private HttpContent _httpContent;
        private IEnumerable<FilterInfo> _filters;
        private Uri _requestUri;
        private HttpMethod _method;
        private bool _hasBuild = false;
        private Mock<UrlHelper> _urlHelperMock;
        private IDependencyScope _scope;


        public ControllerMocker(T controller)
        {
            _controller = controller;
        }

        public IControllerMocker<T> DependencyScope(IDependencyScope scope)
        {
            _scope = scope;
            return this;
        }


        public IControllerMocker<T> Configure(Action<HttpConfiguration> httpConfigAction)
        {
            _httpConfigAction = httpConfigAction;
            return this;
        }

        public IControllerMocker<T> RequestContent(HttpContent httpContent)
        {
            _httpContent = httpContent;
            return this;
        }

        public IControllerMocker<T> Filters(IEnumerable<FilterInfo> filters)
        {
            _filters = filters;
            return this;
        }

        public IControllerMocker<T> AutoMapFilters()
        {
            _filters = _controller.GetType().GetCustomAttributes(typeof(ExceptionFilterAttribute), true).Select(p => new FilterInfo((IFilter)p, FilterScope.Controller));
            return this;
        }

        public IControllerMocker<T> Request(Uri requestUri, HttpMethod method)
        {
            _requestUri = requestUri;
            _method = method;
            return this;
        }

        public IControllerMocker<T> UrlHelperLink(string url)
        {
            _urlHelperMock = new Mock<UrlHelper>();
            _urlHelperMock.Setup(m => m.Link(It.IsAny<string>(), It.IsAny<object>())).Returns(url);
            return this;
        }

        public IControllerMocker<T> UrlHelperCustom(Action<Mock<UrlHelper>> urlHelperMockAction)
        {
            _urlHelperMock = new Mock<UrlHelper>();
            urlHelperMockAction(_urlHelperMock);
            return this;
        }

        public IControllerMocker<T> Build()
        {

            var request = new HttpRequestMessage(_method, _requestUri);

            if (_scope != null)
                request.Properties.Add(HttpPropertyKeys.DependencyScope, _scope);

            if (_httpContent != null)
                request.Content = _httpContent;
            var configuration = new HttpConfiguration();

            request.SetConfiguration(configuration);

            if (_httpConfigAction != null)
                _httpConfigAction(configuration);

            configuration.EnsureInitialized();

            var httpRouteData = configuration.Routes.GetRouteData(request);
            request.SetRouteData(httpRouteData);


            var servicesMock = new Mock<DefaultServices>(configuration) { CallBase = true };

            var filterProviderMock = new Mock<IFilterProvider>();

            if (_filters != null)
                filterProviderMock.Setup(m => m.GetFilters(configuration, It.IsAny<HttpActionDescriptor>()))
                    .Returns(_filters);
            else
                filterProviderMock.Setup(m => m.GetFilters(configuration, It.IsAny<HttpActionDescriptor>()));

            servicesMock.Setup(r => r.GetServices(typeof(IFilterProvider)))
                .Returns(new object[] { filterProviderMock.Object });



            _controller.ControllerContext = new HttpControllerContext(configuration, httpRouteData, request)
            {
                ControllerDescriptor = CreateControllerDescriptor(configuration),
                Controller = _controller
            };

            if (_urlHelperMock != null)
                _controller.Url = _urlHelperMock.Object;

            _hasBuild = true;

            return this;
        }

        public HttpResponseMessage Execute()
        {
            if (!_hasBuild)
                throw new InvalidOperationException("You must call build before execute.");
            return _controller.ExecuteAsync(_controller.ControllerContext,
                CancellationToken.None).GetAwaiter().GetResult();
        }


        private HttpControllerDescriptor CreateControllerDescriptor(HttpConfiguration config)
        {
            var httpControllerDescriptor = new HttpControllerDescriptor()
            {
                Configuration = config,

                ControllerType = typeof(T),
                ControllerName = typeof(T).Name
            };

            return httpControllerDescriptor;
        }
    }
}