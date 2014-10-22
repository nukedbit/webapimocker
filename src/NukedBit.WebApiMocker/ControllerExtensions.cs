using System.Web.Http;

namespace NukedBit.WebApiMocker
{
    public static class ControllerExtensions
    {
        public static IControllerMocker<T> Mocker<T>(this T controller) where T : ApiController
        {
            return new ControllerMocker<T>(controller);
        }
    }
}