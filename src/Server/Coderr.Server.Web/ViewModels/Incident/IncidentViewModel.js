/// <reference path="../../Scripts/Promise.ts" />
/// <reference path="../../Scripts/typings/moment/moment.d.ts" />
/// <reference path="../../Scripts/CqsClient.ts" />
/// <reference path="../ChartViewModel.ts" />
var codeRR;
(function (codeRR) {
    var Incident;
    (function (Incident) {
        var CqsClient = Griffin.Cqs.CqsClient;
        var Pager = Griffin.WebApp.Pager;
        var ReOpenIncident = codeRR.Core.Incidents.Commands.ReOpenIncident;
        var IncidentViewModel = (function () {
            function IncidentViewModel(appScope) {
                this.isIgnored = false;
                console.log(appScope);
            }
            IncidentViewModel.prototype.getTitle = function () {
                codeRR.Applications.Navigation.pageTitle = "Incident '" + this.name + "'";
                return "Incident " + this.name;
            };
            IncidentViewModel.prototype.activate = function (ctx) {
                var _this = this;
                this.ctx = ctx;
                var query = new codeRR.Core.Incidents.Queries.GetIncident(ctx.routeData["incidentId"]);
                CqsClient.query(query)
                    .done(function (response) {
                    window['currentIncident'] = response;
                    IncidentNavigation.set(ctx.routeData);
                    //if (response.IsIgnored) {
                    //    $('#actionButtons').remove();
                    //}
                    //var item = ctx.select.one('#actionButtons');
                    //$(".page-title-box ol").before(item);
                    _this.isIgnored = response.IsIgnored;
                    _this.name = response.Description;
                    _this.id = response.Id;
                    _this.applicationId = response.ApplicationId;
                    _this.pager = new Pager(0, 0, 0);
                    _this.pager.subscribe(_this);
                    _this.pager.draw(ctx.select.one("#pager"));
                    _this.renderInfo(response);
                    var query = new codeRR.Core.Reports.Queries.GetReportList(_this.id);
                    query.PageNumber = 1;
                    query.PageSize = 20;
                    CqsClient.query(query)
                        .done(function (result) {
                        _this.pager.update(result.PageNumber, result.PageSize, result.TotalCount);
                        _this.renderTable(1, result);
                    });
                    var elem = ctx.select.one("#myChart");
                    ctx.handle.click("#reopenBtn", function (e) {
                        e.preventDefault();
                        CqsClient.command(new ReOpenIncident(_this.id))
                            .done(function (e) {
                            window.location.reload();
                        });
                    });
                    ctx.resolve();
                    var el = document.getElementById("pageMenuSource");
                    if (el) {
                        $('#pageMenu').html(el.innerHTML);
                        el.parentElement.removeChild(el);
                    }
                    _this.renderInitialChart(elem, response.DayStatistics);
                });
                ctx.handle.change('[name="range"]', function (e) { return _this.onRange(e); });
            };
            IncidentViewModel.prototype.deactivate = function () {
            };
            IncidentViewModel.prototype.onPager = function (pager) {
                var _this = this;
                var query = new codeRR.Core.Reports.Queries.GetReportList(this.id);
                query.PageNumber = pager.currentPage;
                query.PageSize = 20;
                CqsClient.query(query)
                    .done(function (result) {
                    _this.renderTable(1, result);
                });
            };
            IncidentViewModel.prototype.onRange = function (e) {
                var elem = e.target;
                var days = parseInt(elem.value, 10);
                this.loadChartInfo(days);
            };
            IncidentViewModel.prototype.renderInitialChart = function (chartElement, stats) {
                var data = [];
                for (var i = 0; i < stats.length; ++i) {
                    var dataItem = {
                        date: stats[i].Date,
                        Reports: stats[i].Count
                    };
                    data.push(dataItem);
                }
                $('#myChart').html('');
                this.chartOptions = {
                    element: 'myChart',
                    data: data,
                    xkey: 'date',
                    ykeys: ['Reports'],
                    labels: ['Reports'],
                    lineColors: ['#0094DA']
                };
                this.chart = Morris.Line(this.chartOptions);
            };
            IncidentViewModel.prototype.renderTable = function (pageNumber, data) {
                var self = this;
                var directives = {
                    Items: {
                        CreatedAtUtc: {
                            text: function (value, dto) {
                                return moment(value).fromNow();
                            }
                        },
                        Message: {
                            text: function (value, dto) {
                                if (!value) {
                                    return "(No exception message)";
                                }
                                return dto.Message;
                            },
                            href: function (value, dto) {
                                return "#/application/" + self.applicationId + "/incident/" + self.id + "/report/" + dto.Id;
                            }
                        }
                    }
                };
                this.ctx.renderPartial("#reportsTable", data, directives);
            };
            IncidentViewModel.prototype.renderInfo = function (dto) {
                if (dto.IsSolved) {
                    dto.Tags.push("solved");
                }
                if (dto.IsIgnored) {
                    dto.Tags.push("ignored");
                }
                var directives = {
                    CreatedAtUtc: {
                        text: function (value) {
                            return moment(value).fromNow();
                        }
                    },
                    UpdatedAtUtc: {
                        text: function (value) {
                            return moment(value).fromNow();
                        }
                    },
                    SolvedAtUtc: {
                        text: function (value) {
                            return moment(value).fromNow();
                        }
                    },
                    Tags: {
                        "class": function (value) {
                            if (value === "solved" || value === "ignored")
                                return "label label-danger m-r-5";
                            return "label label-default m-r-5";
                        },
                        text: function (value) {
                            return value;
                        },
                        href: function (value) {
                            if (value.substr(0, 2) !== 'v-' || value !== "ignored" || value !== "solved")
                                return "http://stackoverflow.com/search?q=%5B" + value + "%5D+" + dto.Description;
                        }
                    }
                };
                this.ctx.render(dto, directives);
            };
            IncidentViewModel.prototype.loadChartInfo = function (days) {
                var _this = this;
                var query = new codeRR.Core.Incidents.Queries.GetIncidentStatistics();
                query.IncidentId = this.id;
                query.NumberOfDays = days;
                CqsClient.query(query)
                    .done(function (response) {
                    var data = [];
                    var lastHour = -1;
                    var date = new Date();
                    date.setDate(date.getDate() - 1);
                    for (var i = 0; i < response.Values.length; ++i) {
                        var dataItem = {
                            date: response.Labels[i],
                            Reports: response.Values[i]
                        };
                        if (days === 1) {
                            var hour = parseInt(dataItem.date.substr(0, 2), 10);
                            if (hour < lastHour && lastHour !== -1) {
                                date = new Date();
                            }
                            lastHour = hour;
                            var minute = parseInt(dataItem.date.substr(3, 2), 10);
                            date.setHours(hour, minute);
                            dataItem.date = date.toISOString();
                        }
                        data.push(dataItem);
                    }
                    _this.chartOptions.xLabelFormat = null;
                    if (days === 7) {
                        _this.chartOptions.xLabelFormat = function (xDate) {
                            console.log(moment(xDate).format('dd'));
                            return moment(xDate).format('dd');
                        };
                    }
                    else if (days === 1) {
                        _this.chartOptions.xLabels = "hour";
                    }
                    else {
                        _this.chartOptions.xLabels = "day";
                    }
                    _this.chart.setData(data, true);
                });
            };
            return IncidentViewModel;
        }());
        IncidentViewModel.UP = "fa-chevron-up";
        IncidentViewModel.DOWN = "fa-chevron-down";
        Incident.IncidentViewModel = IncidentViewModel;
    })(Incident = codeRR.Incident || (codeRR.Incident = {}));
})(codeRR || (codeRR = {}));
//# sourceMappingURL=IncidentViewModel.js.map