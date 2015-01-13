using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using System.Web.Http.Hosting;
using Moq;
using NukedBit.WebApiMocker;
using NUnit.Framework;
using SharpTestsEx;

namespace Examples
{
    [TestFixture]
    public class ControllerMockerTests
    {
        [Test]
        public void BuildExecute()
        {
            const string expected = "mymessage";

            var simpleController = new SimpleController();
            var response = simpleController
                .Mocker()
                .Configure(c => c.MapHttpAttributeRoutes())
                .Request(new Uri("http://localhost/simpleget/mymessage"), HttpMethod.Get)
                .Build()
                .Execute();

            response.EnsureSuccessStatusCode(); 

            var simpleResponse = response.Content.ReadAsAsync<SimpleResponse>().GetAwaiter().GetResult();

            Assert.That(simpleResponse.Message, Is.EqualTo(expected));
        }



        [Test]
        public void GetDependencyScope()
        {
            const string expected = "mymessage";

            var dependencyScopeMock = new Mock<IDependencyScope>();
            object myService = "test";
            dependencyScopeMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(myService);

            var simpleController = new SimpleController();
            var response = simpleController
                .Mocker()
                .Configure(c => c.MapHttpAttributeRoutes())
                .DependencyScope(dependencyScopeMock.Object)
                .Request(new Uri("http://localhost/dependencyscope"), HttpMethod.Get)
                .Build()
                .Execute();

            response.EnsureSuccessStatusCode(); 
            var service = response.Content.ReadAsAsync<IDependencyScope>().GetAwaiter().GetResult();
            Assert.AreEqual(service.GetService(typeof(object)), myService);
        }

        [Test]
        public void ExecuteWithoutBuildFail()
        {
            var simpleController = new SimpleController();

            Assert.Throws<InvalidOperationException>(() => simpleController
                .Mocker()
                .Execute()).Message.Should().Be.EqualTo("You must call build before execute.");
        }

        [Test]
        public void BuildWithFiltersExecute()
        {   
            var simpleController = new SimpleController();
            var response = simpleController
                .Mocker()
                .Configure(c => c.MapHttpAttributeRoutes())
                .Filters(new[] { new FilterInfo(new CustomExceptionFilterAttribute(), FilterScope.Controller), })
                .Request(new Uri("http://localhost/simpleget/mymessage?nullArg="), HttpMethod.Get)
                .Build()
                .Execute(); 

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }


        [Test]
        public void BuildWithAutoMapFiltersExecute()
        {   
            var simpleController = new SimpleController();
            var response = simpleController
                .Mocker()
                .Configure(c => c.MapHttpAttributeRoutes())
                .AutoMapFilters()
                .Request(new Uri("http://localhost/simpleget/mymessage?nullArg="), HttpMethod.Get)
                .Build()
                .Execute(); 

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }


        [Test]
        public void UrlHelper()
        {
            var simpleController = new SimpleController();
            const string expected = "http://localhost/simpleget/prova";
            simpleController
                .Mocker()
                .Configure(c => c.MapHttpAttributeRoutes())
                .Request(new Uri("http://localhost/simpleget/prova"), HttpMethod.Get)
                .UrlHelperLink(expected)
                .AutoMapFilters()
                .Build();

           string result =  simpleController.Url.Link("route", new {message = "prova"});

           Assert.That(result, Is.EqualTo(expected));
        }
    }



    public class CustomExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception is ArgumentNullException)
            {
                actionExecutedContext.Response =
                    actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "error");
            }
            base.OnException(actionExecutedContext);
        }
    }

    public class SimpleResponse
    {
        public string Message { get; set; }
    }

    [CustomExceptionFilterAttribute]
    public class SimpleController : ApiController
    {
        [Route("simpleget/{message}",Name = "route")]
        [HttpGet]
        public HttpResponseMessage Get(string message)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new SimpleResponse() { Message = message });
        }

        [Route("simpleget/{message}")]
        [HttpGet]
        public HttpResponseMessage Get(string message, string nullArg)
        {
            if (string.IsNullOrEmpty(nullArg))
                throw new ArgumentNullException("nullArg");
            return Request.CreateResponse(HttpStatusCode.OK, new SimpleResponse() { Message = message });
        }

        [Route("dependencyscope")]
        [HttpGet]
        public IDependencyScope Get()
        {
            return Request.GetDependencyScope();
        }
    }
}
