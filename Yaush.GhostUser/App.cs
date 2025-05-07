using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yaush.GhostUser.Url;

namespace Yaush.GhostUser
{
    internal class App(IUrlCreatorService _urlPopulatorService)
    {
        public async Task<CreateShortenUrlResponse> Run(string input)
        {
            return await _urlPopulatorService.CreateShortenedUrl(input);
        }
    }
}
