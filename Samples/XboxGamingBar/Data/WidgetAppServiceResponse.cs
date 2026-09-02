using Shared.Data;
using Windows.ApplicationModel.AppService;

namespace XboxGamingBar.Data
{
    internal class WidgetAppServiceResponse : SharedAppServiceResponse
    {
        public AppServiceResponse AppServiceResponse { get; }

        public override SharedValueSet Message
        {
            get
            {
                if (AppServiceResponse?.Message != null)
                {
                    return new WidgetValueSet(AppServiceResponse.Message);
                }
                return null;
            }
        }

        public WidgetAppServiceResponse(AppServiceResponse appServiceResponse)
        {
            AppServiceResponse = appServiceResponse;
        }
    }
}
