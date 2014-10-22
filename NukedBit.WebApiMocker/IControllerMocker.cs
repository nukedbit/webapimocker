using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Services;
using Moq;

namespace NukedBit.WebApiMocker
{
    public interface IControllerMocker<out T> where T : ApiController
    {
        IControllerMocker<T> Configure(Action<HttpConfiguration> httpConfigAction);

        IControllerMocker<T> RequestContent(HttpContent httpContent);

        IControllerMocker<T> Filters(IEnumerable<FilterInfo> filters);

        IControllerMocker<T> Request(Uri requestUri, HttpMethod method);

        IControllerMocker<T> Build();
        
        HttpResponseMessage Execute();
    }


    public static class ControllerExtensions
    {
        public static IControllerMocker<T> Mocker<T>(this T controller) where T : ApiController
        {
            return new ControllerMocker<T>(controller);
        }
    }

    internal class ControllerMocker<T> : IControllerMocker<T> where T : ApiController
    {
        private readonly T _controller;

        private Action<HttpConfiguration> _httpConfigAction;
        private HttpContent _httpContent;
        private IEnumerable<FilterInfo> _filters;
        private Uri _requestUri;
        private HttpMethod _method;

        public ControllerMocker(T controller)
        {
            _controller = controller;
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

        public IControllerMocker<T> Request(Uri requestUri, HttpMethod method)
        {
            _requestUri = requestUri;
            _method = method;
        }

        public IControllerMocker<T> Build()
        {

            var request = new HttpRequestMessage(_method, _requestUri);
            if (_httpContent != null)
                request.Content = _httpContent
                    ;
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

            servicesMock.Setup(r => r.GetServices(typeof(IFilterProvider)))
            .Returns(new object[] { filterProviderMock.Object });


            _controller.ControllerContext = new HttpControllerContext(configuration, httpRouteData, request)
            {
                ControllerDescriptor = CreateControllerDescriptor(configuration),
                Controller = _controller
            };

            return this;
        }

        public HttpResponseMessage Execute()
        {
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
