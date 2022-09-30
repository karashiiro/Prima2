using Google.Apis.Http;

namespace Prima.Application.Scheduling.Calendar;

public class DummyCredential : IConfigurableHttpClientInitializer
{
    public void Initialize(ConfigurableHttpClient httpClient)
    {
    }
}