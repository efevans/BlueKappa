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
        public async Task<CreateShortenUrlResponse> Run()
        {
            return await _urlPopulatorService.CreateShortenedUrl(GetRandomUrl());
        }

        private static string GetRandomUrl()
        {
            var random = new Random();
            int val = random.Next() % 9;

            return val switch
            {
                < 2 => "https://reddit.com",
                < 5 => "https://google.com",
                < 6 => "https://wikipedia.com",
                _ => "https://youtube.com"
            };
        }
    }
}
