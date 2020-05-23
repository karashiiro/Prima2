using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Prima.Models;
using Prima.Resources;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prima.Services;
using Color = Google.Apis.Sheets.v4.Data.Color;

namespace Prima.Scheduler.Services
{
    public class SpreadsheetService : IDisposable
    {
        private const string ApplicationName = "Prima";
        private const int Delay = 1800000;

        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        private static string GCredentialsFile => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "credentials.json" // Only use Windows for testing.
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "credentials.json");
        private static string GTokenFile => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "token.json"
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "token.json");

        private readonly DbService _db;
        private readonly SheetsService _service;
        private readonly CancellationTokenSource _tokenSource;

        public SpreadsheetService(DbService db)
        {
            _db = db;
            _tokenSource = new CancellationTokenSource();

            using var stream = new FileStream(GCredentialsFile, FileMode.Open, FileAccess.Read);
            // ReSharper disable once AsyncConverter.AsyncWait
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(GTokenFile, true)
            ).Result;

            Log.Information("Credential file saved to {File}", GTokenFile);

            _service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            _ = Task.Run(() => AddFutureRunLoop(_tokenSource.Token));
        }

        public async Task AddEvent(ScheduledEvent @event, string spreadsheetId)
        {
            var dateObj = DateTime.FromBinary(@event.RunTime);

            var constants = await GetSpreadsheet(spreadsheetId, "Constants!B1:B3");

            var timeslotsPerDay = int.Parse((string)constants.Values[1][0]);
            var dateRow = int.Parse((string)constants.Values[2][0]);

            var range = $"Timetable!{dateRow}:{dateRow + timeslotsPerDay}";
            var timetable = await GetSpreadsheet(spreadsheetId, range);

            var row = dateRow + dateObj.Hour * 2 + (dateObj.Minute == 30 ? 1 : 0);
            var column = -1;

            var dateRowContents = timetable.Values[0];
            for (var i = 0; i < dateRowContents.Count; i++)
            {
                if (((string)dateRowContents[i]).Contains(dateObj.Day.ToString()))
                {
                    column = i;
                    break;
                }
            }

            if (column == -1)
                return; // Try again later.

            var color = RunDisplayTypes.GetColor(@event.RunKind);
            var batchRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        UpdateCells = new UpdateCellsRequest
                        {
                            Rows = new List<RowData>
                            {
                                new RowData
                                {
                                    Values = new List<CellData>
                                    {
                                        new CellData
                                        {
                                            UserEnteredFormat = new CellFormat
                                            {
                                                BackgroundColor = new Color
                                                {
                                                    Red = color.RGB[0] / 255.0f,
                                                    Green = color.RGB[1] / 255.0f,
                                                    Blue = color.RGB[2] / 255.0f,
                                                },
                                            },
                                            UserEnteredValue = new ExtendedValue
                                            {
                                                StringValue = @event.Description,
                                            },
                                        },
                                    },
                                },
                            },
                            Range = new GridRange
                            {
                                SheetId = 0,
                                StartRowIndex = row,
                                EndRowIndex = row + 1,
                                StartColumnIndex = column,
                                EndColumnIndex = column + 1,
                            },
                            Fields = "userEnteredFormat(backgroundColor), userEnteredValue",
                        },
                    },
                },
            };
            var request = _service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId);
            _ = await request.ExecuteAsync();

            @event.Listed = true;
            await _db.UpdateScheduledEvent(@event);
        }

        public async Task RemoveEvent(ScheduledEvent @event, string spreadsheetId)
        {
            var dateObj = DateTime.FromBinary(@event.RunTime);

            var constants = await GetSpreadsheet(spreadsheetId, "Constants!B1:B3");

            var timeslotsPerDay = int.Parse((string)constants.Values[1][0]);
            var dateRow = int.Parse((string)constants.Values[2][0]);

            var range = $"Timetable!{dateRow}:{dateRow + timeslotsPerDay}";
            var timetable = await GetSpreadsheet(spreadsheetId, range);

            var row = dateRow + dateObj.Hour * 2 + (dateObj.Minute == 30 ? 1 : 0);
            var column = -1;

            var dateRowContents = timetable.Values[0];
            for (var i = 0; i < dateRowContents.Count; i++)
            {
                if (((string)dateRowContents[i]).Contains(dateObj.Day.ToString()))
                {
                    column = i;
                    break;
                }
            }

            var batchRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        UpdateCells = new UpdateCellsRequest
                        {
                            Rows = new List<RowData>
                            {
                                new RowData
                                {
                                    Values = new List<CellData>
                                    {
                                        new CellData
                                        {
                                            UserEnteredFormat = new CellFormat
                                            {
                                                BackgroundColor = row % 2 == 0 ? new Color
                                                {
                                                    Red = 1.0f,
                                                    Green = 1.0f,
                                                    Blue = 1.0f,
                                                } : new Color
                                                {
                                                    Red = 243 / 255.0f,
                                                    Green = 243 / 255.0f,
                                                    Blue = 243 / 255.0f,
                                                },
                                            },
                                            UserEnteredValue = new ExtendedValue
                                            {
                                                StringValue = "",
                                            },
                                        },
                                    },
                                },
                            },
                            Range = new GridRange
                            {
                                SheetId = 0,
                                StartRowIndex = row,
                                EndRowIndex = row + 1,
                                StartColumnIndex = column,
                                EndColumnIndex = column + 1,
                            },
                            Fields = "userEnteredFormat(backgroundColor), userEnteredValue",
                        },
                    },
                },
            };
            var request = _service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId);
            _ = await request.ExecuteAsync();
        }

        private async Task AddFutureRunLoop(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                var runs = _db.Events.Where(e => e.RunTime > DateTime.Now.ToBinary() && !e.Notified && !e.Listed);
                foreach (var run in runs)
                {
                    var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == run.GuildId);
                    if (guildConfig == null)
                        continue;
                    await AddEvent(run, guildConfig.BASpreadsheetId);
                    await Task.Delay(1000, token);
                }

                await Task.Delay(Delay, token);
            }
        }

        private Task<ValueRange> GetSpreadsheet(string spreadSheetId, string range)
        {
            var request = _service.Spreadsheets.Values.Get(spreadSheetId, range);
            return request.ExecuteAsync();
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
}
