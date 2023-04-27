using Azure.Core;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using RestSharp;
using Sonzea.Excel.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sonzea.Excel.API.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenAIService
    {
        private readonly String _api;
        private readonly RestSharp.RestClient _client;
        private readonly RestSharp.RestRequest _request;
        /// <summary>
        /// 
        /// </summary>
        public OpenAIService()
        {
            _api = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _client = new RestClient("https://a334aab5-797a-456c-9807-a2a14f55ed20.mock.pstmn.io");
            _request = new RestRequest("chat/completions", Method.Post);
            _request.AddHeader("Content-Type", "application/json");
            _request.AddHeader("Authorization", $"Bearer {_api}");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<object> Classify(Prompt prompt)
        {
            string pattern = @"(\d+)";
            string nameList = string.Join("\n", prompt.Category.Select((n, i) => i.ToString() + "." + n.Name));
            object system_message1 = new { role = "system", content = $"please classify user input into one of the categories {nameList}" };
            object system_message2 = new { role = "system", content = "only answer category serial number" };
            object user_message = new { role = "user", content = String.Join(",", prompt.Records) };

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(system_message1);
            Console.WriteLine(system_message2);
            Console.WriteLine(user_message);

            object[] messages = new[] { system_message1, system_message2, user_message };
            var result = await ChatGPTCompletion(messages);


            Match match = Regex.Match(result, pattern);
            if (match.Success)
            {
                int index1 = int.Parse(match.Groups[1].Value);
                nameList = string.Join("\n", prompt.Category[index1].Children.Select((n, i) => i.ToString() + "." + n));
                if (nameList == null)
                {
                    var first_category = prompt.Category[index1].Name;
                    return new { success = true, classifiedRecords = new { name = first_category } };
                }

                system_message1 = new { role = "system", content = $"please classify user input into one of the categories {nameList}" };
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(system_message1);
                Console.WriteLine(system_message2);
                Console.WriteLine(user_message);
                messages = new[] { system_message1, system_message2, user_message };
                result = await ChatGPTCompletion(messages);
                match = Regex.Match(result, pattern);
                if (match.Success)
                {
                    var first_category = prompt.Category[index1].Name;
                    int index2 = int.Parse(match.Groups[1].Value);
                    var second_category = prompt.Category[index1].Children[index2];
                    return new { success = true, classifiedRecords = new[] { new { name = first_category, children = second_category } } };

                }
                else
                {
                    return new { success = false, message = prompt.Category[index1].Name };

                }
            }
            else
            {
                return new { success = false, message = result };

            }

        }

        private async Task<string> ChatGPTCompletion(object[] msg)
        {
            var requestContent = new
            {
                model = "gpt-3.5-turbo",
                messages = msg
            };
            _request.AddJsonBody(requestContent);

            var responseRestSharp = await _client.ExecuteAsync(_request);

            if (responseRestSharp.IsSuccessful)
            {
                string content = responseRestSharp.Content;
                var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(content);
                return response.Choices[0].Message.Content;
            }
            else
            {
                throw new Exception("Request failed: " + responseRestSharp.ErrorMessage);
            }
        }



    }
}
