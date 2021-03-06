# Web Api Controller Mocking Helpers

[![Build status](https://ci.appveyor.com/api/projects/status/grvxvo7ax034vfyy?svg=true)](https://ci.appveyor.com/project/nukedbit/webapimocker)

Why this framework?

Lets say for example, you have a controller with a custom attribute for error handling.
How do you build a test for it? Usually with Http Self Host right? 

But no more!! with a few line of code, you can get a controller with a mocked http stack.


### You can specify manually wich IFilter to use

		 var simpleController = new SimpleController();
					var response = simpleController
						.Mocker()
						.Configure(c => c.MapHttpAttributeRoutes())
						.Filters(new[] { new FilterInfo(new CustomExceptionFilterAttribute(), FilterScope.Controller), })
						.Request(new Uri("http://localhost/simpleget/mymessage?nullArg="), HttpMethod.Get)
						.Build()
						.Execute(); 

## Or you can Auto Map Filters


            var simpleController = new SimpleController();
            var response = simpleController
                .Mocker()
                .Configure(c => c.MapHttpAttributeRoutes())
                .AutoMapFilters()
                .Request(new Uri("http://localhost/simpleget/mymessage?nullArg="), HttpMethod.Get)
                .Build()
                .Execute(); 

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));						
						

## Installation

    PM> Install-Package NukedBit.WebApiMocker


