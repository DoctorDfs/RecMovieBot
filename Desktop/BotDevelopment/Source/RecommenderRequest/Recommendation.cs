// This code requires the Nuget package Microsoft.AspNet.WebApi.Client to be installed.
// Instructions for doing this in Visual Studio:
// Tools -> Nuget Package Manager -> Package Manager Console
// Install-Package Microsoft.AspNet.WebApi.Client

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RecommenderRequest
{
    public class Recommendation
    {
        public static async Task<string> InvokeRequestResponseRecommendationService(int idUser)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>>()
                    {
                    },
                    GlobalParameters = new Dictionary<string, string>() {
                            {
                                "Database query", "select * " +
                                                  "from users left join movie_rating on users.id_user=movie_rating.id_user " +
                                                  "left join movie on movie_rating.id_movie=movie.id_movie " +
                                                  "left join director_rating on director_rating.id_user=users.id_user " +
                                                  "left join genre_rating on genre_rating.id_user=users.id_user " +
                                                  "left join actor_rating on actor_rating.id_user=users.id_user "
                            },
                            {
                                "Fraction of training-only users", "0.3"
                            },
                            {
                                "Fraction of test user ratings for training", "0.7"
                            },
                    }
                };

                const string apiKey = "WHYLuugnhW0ufiSqxPu/0ldypGKY5jMnAAfMi3ZaRm3J9VPvguR/OZpesbNPDetXofDlRG+dvfjDaWbp7JSmRg=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri("https://uswestcentral.services.azureml.net/subscriptions/dba37ecf66f64b7e825abc8cbd365998/services/22d1ccd130d24cb2a05dff0790245912/execute?api-version=2.0&format=swagger");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Debug.Print("Result: {0}", result);

                    //parserizzo il contenuto
                    return result;

                


                }
                else
                {
                    Debug.Print(string.Format("The request failed with status code: {0}", response.StatusCode));

                    Debug.Print(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Print(responseContent);
                }
            }
            return null;
        }
    }
}