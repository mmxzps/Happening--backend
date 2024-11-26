﻿using Azure.Core;
using EventVault.Models;
using EventVault.Models.DTOs;
using EventVault.Models.ViewModels;
using EventVault.Services.IServices;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace EventVault.Services
{
    public class KBEventServices : IKBEventServices
    {
        private readonly HttpClient _httpClient;

        public KBEventServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        readonly string apiKey = Environment.GetEnvironmentVariable("KulturApiKey");

        public async Task<List<EventViewModel>> GetEventDataAsync()
        {
            var request1 = new HttpRequestMessage(HttpMethod.Get, "https://kulturbiljetter.se/api/v3/events/");
            request1.Headers.Add("Authorization", $"Token {apiKey}");
            var response1 = await _httpClient.SendAsync(request1);

            List<KBEventViewModel> responseList = new List<KBEventViewModel>();

            if (response1.IsSuccessStatusCode)
            {

                var jsonData = await response1.Content.ReadAsStringAsync();

                var parsedObject = JObject.Parse(jsonData);

                //var eventList = JsonConvert.DeserializeObject<List<KBEventViewModel>>(jsonData);

                var eventList = parsedObject.Properties()
                                            .Select(prop => prop.Value.ToObject<KBEventListViewModel>())
                                            .ToList();

                var eventIds = eventList.Select(x=>x.event_id).ToList(); 
                
                for(int i = 0; i < eventIds.Count; i++)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"https://kulturbiljetter.se/api/v3/events/{eventIds[i]}");
                    string eTag = null;


                    request.Headers.Add("Authorization", $"Token {apiKey}");

                    if (!string.IsNullOrEmpty(eTag))
                    {
                        request.Headers.Add("If-None-Match", $"{eTag}");
                    }


                    var response = await _httpClient.SendAsync(request);


                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        return null; // No changes
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(jsonString);
                        var eventViewModel = JsonConvert.DeserializeObject<KBEventViewModel>(jsonString);

                        responseList.Add(eventViewModel);
                    }
                    else
                    {
                        throw new Exception($"Failed to fetch data. Error {response.StatusCode}");
                    }
                }

            }

            var EventViewModels = new List<EventViewModel>();

            foreach (KBEventViewModel eventResponse in responseList)
            {
                var eventViewModel = new EventViewModel()
                {
                    EventId = eventResponse.organizer.organizer_id.ToString() ?? "",
                    Title = eventResponse.title ?? "",
                    Description = eventResponse.presentation_short ?? "",
                    HighestPrice = eventResponse.price_max,
                    LowestPrice = eventResponse.price_min,
                    EventUrlPage = eventResponse.url_event_page ?? ""
                    
                };

                //adds the first image from the event to eventViewModel
                if (eventResponse.images != null && eventResponse.images.ContainsKey("0")) 
                {
                    eventViewModel.ImageUrl = eventResponse.images["0"] ?? "";                    
                }

                //add releasetime of tickets to viewmodel
                if (eventResponse.unixtime_release != null)
                {
                    eventViewModel.ticketsRelease = DateTimeOffset.FromUnixTimeSeconds(eventResponse.unixtime_release).DateTime;
                }

                //add dates to viewmodel
                if (eventResponse.dates != null)
                {
                    eventViewModel.Dates = await ConvertEventDates(eventResponse.dates);
                }

                //add venue to viewmodel
                if (eventResponse.locations != null && eventResponse.locations.Any())
                {
                    eventViewModel.Venue = new VenueViewModel
                    {
                        Name = eventResponse.locations.First().Value.name ?? "",
                        Address = eventResponse.locations.First().Value.street ?? "",
                        City = eventResponse.locations.First().Value.city ?? "",
                    };
                }

                //clean description of htmltags.
                if (!eventViewModel.Description.IsNullOrEmpty())
                {
                    eventViewModel.Description = Regex.Replace(eventViewModel.Description, "<.*?>", string.Empty);

                    eventViewModel.Description = eventViewModel.Description.Replace("&nbsp;", "");

                    eventViewModel.Description = eventViewModel.Description.Replace("&amp;", "&");

                    eventViewModel.Description = eventViewModel.Description.Replace("&quot;", "\"");
                }

                //add viewmodel to list of viewmodels.
                EventViewModels.Add(eventViewModel);
            }

            
            return EventViewModels;

            //returning viewModels instead of 
            //return responseList;
        }

        public async Task<IEnumerable<KBEventListViewModel>> GetListOfEventsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://kulturbiljetter.se/api/v3/events/");



            request.Headers.Add("Authorization", $"Token {apiKey}");

            var response = await _httpClient.SendAsync(request);


            if (response.IsSuccessStatusCode)
            {

                var jsonData = await response.Content.ReadAsStringAsync();

                var parsedObject = JObject.Parse(jsonData);

                //var eventList = JsonConvert.DeserializeObject<List<KBEventViewModel>>(jsonData);

                var eventList = parsedObject.Properties()
                                            .Select(prop => prop.Value.ToObject<KBEventListViewModel>())
                                            .ToList();

                return eventList;
            }

            else
            {
                throw new Exception($"Failed to fetch data. Error {response.StatusCode}");
            }
        }

        public async Task<List<DateTime>> ConvertEventDates(Dictionary<string, EventDate> eventDates)
        {
            var dateList = new List<DateTime>();

            foreach (var entry in eventDates.Values)
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(entry.unixtime_start).DateTime;

                dateList.Add(date);
            }

            return dateList;
        }
    }
}

