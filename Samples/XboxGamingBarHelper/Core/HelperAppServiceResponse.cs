using Shared.Data;
using Windows.ApplicationModel.AppService;

namespace XboxGamingBarHelper.Core
{
    internal class HelperAppServiceResponse : SharedAppServiceResponse
    {
        public AppServiceResponse AppServiceResponse { get; }

        public override SharedValueSet Message
        {
            get
            {
                if (AppServiceResponse?.Message != null)
                {
                    return new HelperValueSet(AppServiceResponse.Message);
                }
                return null;
            }
        }

        public HelperAppServiceResponse(AppServiceResponse appServiceResponse)
        {
            AppServiceResponse = appServiceResponse;
        }
    }
}
