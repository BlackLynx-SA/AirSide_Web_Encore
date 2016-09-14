/// <reference path="../../typings/jquery/jquery.d.ts" />
var AirSide;
(function (AirSide) {
    var SharedHelper;
    (function (SharedHelper) {
        //Enums
        var SeverityLevels;
        (function (SeverityLevels) {
            SeverityLevels[SeverityLevels["Critical"] = 1] = "Critical";
            SeverityLevels[SeverityLevels["Major"] = 2] = "Major";
            SeverityLevels[SeverityLevels["Minor"] = 3] = "Minor";
            SeverityLevels[SeverityLevels["None"] = 4] = "None";
        })(SeverityLevels || (SeverityLevels = {}));
        var SurveyorAlerts = (function () {
            function SurveyorAlerts() {
                this.$criticalCount = $('#svCritical');
                this.$majorCount = $('#svMajor');
                this.$minorCount = $('#svMinor');
                this.$total = $('#svTotal');
                this.getAlerts();
            }
            SurveyorAlerts.prototype.getAlerts = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Surveyor/getAlertSummary",
                    success: function (json) {
                        var total = 0;
                        json.forEach(function (c) {
                            switch (c.AlertType.severity) {
                                case SeverityLevels.Critical:
                                    _this.$criticalCount.html(c.AlertCount.toString());
                                    total += c.AlertCount;
                                    break;
                                case SeverityLevels.Major:
                                    _this.$majorCount.html(c.AlertCount.toString());
                                    total += c.AlertCount;
                                    break;
                                case SeverityLevels.Minor:
                                    _this.$minorCount.html(c.AlertCount.toString());
                                    total += c.AlertCount;
                                    break;
                                default:
                                    break;
                            }
                        });
                        _this.$total.html(total.toString());
                    }
                });
            };
            return SurveyorAlerts;
        }());
        SharedHelper.SurveyorAlerts = SurveyorAlerts;
    })(SharedHelper = AirSide.SharedHelper || (AirSide.SharedHelper = {}));
})(AirSide || (AirSide = {}));
$(document).on('ready', function (c) {
    var surveyorAlerts = new AirSide.SharedHelper.SurveyorAlerts();
});
