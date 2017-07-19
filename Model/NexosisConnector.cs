using Microsoft.Extensions.Options;
using Nexosis.Api.Client;

namespace NexosisFitbit.Model
{
    public class NexosisConnector
    {
        private readonly IOptions<NexosisSettings> nexosisConfig;

        public NexosisConnector(IOptions<NexosisSettings> nexosisConfig)
        {
            this.nexosisConfig = nexosisConfig;
        }
        
        public INexosisClient Connect()
        {
            return new Nexosis.Api.Client.NexosisClient(nexosisConfig.Value.ApiKey, nexosisConfig.Value.Url);
        } 
    }
}