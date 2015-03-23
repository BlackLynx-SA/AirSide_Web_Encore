/// <reference path="../../typings/jquery/jquery.d.ts" />

module AirSide.SharedHelper {

    //Enums
    enum SeverityLevels {
        Critical = 1,
        Major = 2,
        Minor = 3,
        None = 4
    }

    interface IAlertSummary {
        AlertType: ISeverity;
        AlertCount: number;
    }

    interface ISeverity {
        severity: SeverityLevels;
    }

    export class SurveyorAlerts {
        $criticalCount = $('#svCritical');
        $majorCount = $('#svMajor');
        $minorCount = $('#svMinor');
        $total = $('#svTotal');

        constructor(){
            this.getAlerts();
        }

        private getAlerts() {
            $.ajax({
                type: "POST",
                url: "../../Surveyor/getAlertSummary",
                success: (json: Array<IAlertSummary>) => {
                    var total: number = 0;
                    json.forEach(c=> {
                        switch (c.AlertType.severity) {
                            case SeverityLevels.Critical:
                                this.$criticalCount.html(c.AlertCount.toString());
                                break;
                            case SeverityLevels.Major:
                                this.$majorCount.html(c.AlertCount.toString());
                                break;
                            case SeverityLevels.Minor:
                                this.$minorCount.html(c.AlertCount.toString());
                                break;
                            default:
                                break;
                        }

                        total += c.AlertCount;
                    });

                    this.$total.html(total.toString());
                }
            });
        }
    }
} 

$(document).on('ready', c=> {
    var SurveyorAlerts = new AirSide.SharedHelper.SurveyorAlerts();
});