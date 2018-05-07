using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LuisBot.ProcessTextRequest
{
    public static class ProcessText
    {
        public static async Task<LinkedList<string>> InvokeRequestResponseService(string query)
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
                                "Data", query
                            },
                    }
                };

                const string apiKey = "dcUDrzkY5Ba2w9bvwEcwmWTaFuKS4wg1/p+XjWq80rHxlvAf0bggrFwAJP7Tq1zuZpCU20+h6rXTp9nyzoQEJw=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.BaseAddress = new Uri("https://uswestcentral.services.azureml.net/subscriptions/dba37ecf66f64b7e825abc8cbd365998/services/39bcb26427504524bbbe613ea31c84ff/execute?api-version=2.0&format=swagger");

                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //result = await DoSomeTask();
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    
                    Debug.Print("Result: {0}", result);

                    //get phrase   
                    string stringStart = "\"Preprocessed Col1\":\"";
                    string stringEnd = "\"}]}}";
                    int indexofstartphrases = result.IndexOf(stringStart);

                    int indexofendphrases = result.IndexOf(stringEnd);

                    char[] arrayCharResult = result.ToCharArray();

                    int startCopy = indexofstartphrases + stringStart.Length;
                    int endCopy = indexofendphrases;

                    char[] arrayPhrases = new char[endCopy - startCopy];

                    Array.Copy(arrayCharResult, startCopy, arrayPhrases, 0, endCopy - startCopy);

                    string phrases = new string(arrayPhrases);

                    string[] sentencesFind = phrases.Split('|');
                    LinkedList<string> detecetedSentences = new LinkedList<string>();
                    int i = 0;
                    while (i < sentencesFind.Length)
                    {
                        if (sentencesFind[i].Length != 0)
                        {
                            detecetedSentences.AddLast(sentencesFind[i].Trim());
                            Debug.Print($"Frase rilevata: {detecetedSentences.Last.Value}");
                        }
                        i++;
                    }
                    return detecetedSentences;
                }
                else
                {
                    Debug.Print(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Debug.Print(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Print(responseContent);

                    return null;
                }
            }
        }
    }
}






       


