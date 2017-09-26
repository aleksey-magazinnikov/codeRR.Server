/// <reference path="../../Scripts/Promise.ts" />
/// <reference path="../../Scripts/CqsClient.ts" />
/// <reference path="../ChartViewModel.ts" />
/// <reference path="../../Scripts/Griffin.Yo.d.ts" />
var OneTrueError;
(function (OneTrueError) {
    var Feedback;
    (function (Feedback) {
        var CqsClient = Griffin.Cqs.CqsClient;
        var ApplicationService = OneTrueError.Applications.ApplicationService;
        var ApplicationViewModel = (function () {
            function ApplicationViewModel() {
                this.renderDirectives = {
                    Items: {
                        Message: {
                            html: function (value) {
                                return nl2br(value);
                            }
                        },
                        Title: {
                            style: function () {
                                return '';
                            },
                            html: function (value, dto) {
                                console.log(dto);
                                return "Reported for <a href=\"#/application/" + dto.applicationId + "/incident/" + dto.IncidentId + "\">" + dto.IncidentName + "</a> at " + moment(dto.WrittenAtUtc).fromNow();
                            }
                        },
                        EmailAddress: {
                            text: function (value) {
                                return value;
                            },
                            href: function (value) {
                                return "mailto:" + value;
                            }
                        }
                    }
                };
            }
            ApplicationViewModel.prototype.getTitle = function () {
                var appId = this.context.routeData['applicationId'];
                var app = new ApplicationService();
                app.get(appId)
                    .then(function (result) {
                    var bc = [
                        { href: "/application/" + appId + "/", title: result.Name },
                        { href: "/application/" + appId + "/feedback", title: 'Feedback' }
                    ];
                    OneTrueError.Applications.Navigation.breadcrumbs(bc);
                    OneTrueError.Applications.Navigation.pageTitle = 'Feedback';
                });
                return "Feedback";
            };
            ApplicationViewModel.prototype.activate = function (context) {
                var _this = this;
                this.context = context;
                var query = new OneTrueError.Web.Feedback.Queries.GetFeedbackForApplicationPage(context.routeData["applicationId"]);
                CqsClient.query(query)
                    .done(function (result) {
                    _this.dto = result;
                    _this.dto.Items.forEach(function (item) {
                        item['applicationId'] = context.routeData['applicationId'];
                    });
                    context.render(result, _this.renderDirectives);
                    context.resolve();
                });
            };
            ApplicationViewModel.prototype.deactivate = function () {
            };
            return ApplicationViewModel;
        }());
        Feedback.ApplicationViewModel = ApplicationViewModel;
    })(Feedback = OneTrueError.Feedback || (OneTrueError.Feedback = {}));
})(OneTrueError || (OneTrueError = {}));
//# sourceMappingURL=ApplicationViewModel.js.map