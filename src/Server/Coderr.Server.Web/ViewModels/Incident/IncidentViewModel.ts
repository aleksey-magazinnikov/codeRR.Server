﻿/// <reference path="../../Scripts/Promise.ts" />
/// <reference path="../../Scripts/typings/moment/moment.d.ts" />
/// <reference path="../../Scripts/CqsClient.ts" />
/// <reference path="../ChartViewModel.ts" />
module codeRR.Incident {
    import CqsClient = Griffin.Cqs.CqsClient;
    import PagerSubscriber = Griffin.WebApp.IPagerSubscriber;
    import Pager = Griffin.WebApp.Pager;
    import ReportDay = Core.Incidents.Queries.ReportDay;
    import ApplicationService = codeRR.Applications.ApplicationService;
    import ReOpenIncident = codeRR.Core.Incidents.Commands.ReOpenIncident;

    export class IncidentViewModel implements PagerSubscriber, Griffin.Yo.Spa.ViewModels.IViewModel {
        private static UP = "fa-chevron-up";
        private static DOWN = "fa-chevron-down";
        private pager: Pager;
        private applicationId: number;
        private name: string;
        private ctx: Griffin.Yo.Spa.ViewModels.IActivationContext;
        private chartOptions: morris.ILineOptions;
        private chart: morris.GridChart;
        id: number;
        isIgnored = false;

        constructor(appScope) {
            console.log(appScope);

        }


        getTitle(): string {
            Applications.Navigation.pageTitle = `Incident '${this.name}'`;
            return `Incident ${this.name}`;
        }

        activate(ctx: Griffin.Yo.Spa.ViewModels.IActivationContext) {
            this.ctx = ctx;
            const query = new Core.Incidents.Queries.GetIncident(ctx.routeData["incidentId"]);
            CqsClient.query<Core.Incidents.Queries.GetIncidentResult>(query)
                .done(response => {
                    window['currentIncident'] = response;

                    IncidentNavigation.set(ctx.routeData);

                    //if (response.IsIgnored) {
                    //    $('#actionButtons').remove();
                    //}
                    //var item = ctx.select.one('#actionButtons');
                    //$(".page-title-box ol").before(item);

                    this.isIgnored = response.IsIgnored;
                    this.name = response.Description;
                    this.id = response.Id;
                    this.applicationId = response.ApplicationId;
                    this.pager = new Pager(0, 0, 0);
                    this.pager.subscribe(this);
                    this.pager.draw(ctx.select.one("#pager"));
                    this.renderInfo(response);

                    var query = new Core.Reports.Queries.GetReportList(this.id);
                    query.PageNumber = 1;
                    query.PageSize = 20;
                    CqsClient.query<Core.Reports.Queries.GetReportListResult>(query)
                        .done(result => {
                            this.pager.update(result.PageNumber, result.PageSize, result.TotalCount);
                            this.renderTable(1, result);
                        });
                    var elem = ctx.select.one("#myChart") as HTMLCanvasElement;
                    ctx.handle.click("#reopenBtn",
                        e => {
                            e.preventDefault();
                            CqsClient.command(new ReOpenIncident(this.id))
                                .done(e => {
                                    window.location.reload();
                                });
                        });
                    ctx.resolve();
                    var el = document.getElementById("pageMenuSource");
                    if (el) {
                        $('#pageMenu').html(el.innerHTML);
                        el.parentElement.removeChild(el);
                    }


                    this.renderInitialChart(elem, response.DayStatistics);

                });

            ctx.handle.change('[name="range"]', e => this.onRange(e));

        }

        deactivate() {

        }


        onPager(pager: Pager): void {
            const query = new Core.Reports.Queries.GetReportList(this.id);
            query.PageNumber = pager.currentPage;
            query.PageSize = 20;
            CqsClient.query<Core.Reports.Queries.GetReportListResult>(query)
                .done(result => {
                    this.renderTable(1, result);
                });
        }

        private onRange(e: Event) {
            const elem = e.target as HTMLInputElement;
            const days = parseInt(elem.value, 10);
            this.loadChartInfo(days);
        }


        private renderInitialChart(chartElement: HTMLCanvasElement, stats: ReportDay[]) {
            var data = [];
            for (let i = 0; i < stats.length; ++i) {
                const dataItem = {
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
        }

        private renderTable(pageNumber: number, data: any) {
            var self = this;
            const directives = {
                Items: {
                    CreatedAtUtc: {
                        text(value, dto) {
                            return moment(value).fromNow();
                        }
                    },
                    Message: {
                        text(value, dto) {
                            if (!value) {
                                return "(No exception message)";
                            }
                            return dto.Message;
                        },
                        href(value, dto) {
                            return `#/application/${self.applicationId}/incident/${self.id}/report/${dto.Id}`;
                        }
                    }
                }
            };
            this.ctx.renderPartial("#reportsTable", data, directives);
        }

        private renderInfo(dto: Core.Incidents.Queries.GetIncidentResult) {
            if (dto.IsSolved) {
                dto.Tags.push("solved");
            }
            if (dto.IsIgnored) {
                dto.Tags.push("ignored");
            }
            const directives = {
                CreatedAtUtc: {
                    text(value) {
                        return moment(value).fromNow();
                    }
                },
                UpdatedAtUtc: {
                    text(value) {
                        return moment(value).fromNow();
                    }
                },
                SolvedAtUtc: {
                    text(value) {
                        return moment(value).fromNow();
                    }
                },
                Tags: {
                    "class"(value) {
                        if (value === "solved" || value === "ignored")
                            return "label label-danger m-r-5";

                        return "label label-default m-r-5";
                    },
                    text(value) {
                        return value;
                    },
                    href(value) {
                        if (value.substr(0, 2) !== 'v-' || value !== "ignored" || value !== "solved" )
                            return `http://stackoverflow.com/search?q=%5B${value}%5D+${dto.Description}`;
                    }
                }

            };
            this.ctx.render(dto, directives);
        }

        private loadChartInfo(days: number) {
            const query = new Core.Incidents.Queries.GetIncidentStatistics();
            query.IncidentId = this.id;
            query.NumberOfDays = days;
            CqsClient.query<Core.Incidents.Queries.GetIncidentStatisticsResult>(query)
                .done(response => {
                    var data: any[] = [];
                    var lastHour = -1;
                    var date: Date = new Date();
                    date.setDate(date.getDate() - 1);
                    for (let i = 0; i < response.Values.length; ++i) {
                        const dataItem = {
                            date: <any>response.Labels[i],
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

                    this.chartOptions.xLabelFormat = null;
                    if (days === 7) {
                        this.chartOptions.xLabelFormat = (xDate: Date): string => {
                            console.log(moment(xDate).format('dd'));
                            return moment(xDate).format('dd');
                        }
                    } else if (days === 1) {
                        this.chartOptions.xLabels = "hour";
                    } else {
                        this.chartOptions.xLabels = "day";
                    }
                    this.chart.setData(data, true);
                });
        }

    }
}