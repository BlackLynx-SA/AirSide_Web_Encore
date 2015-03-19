/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/flot/jquery.flot.d.ts" />

module AirSide.HomeHelper {
    //Enum
    enum DashboardMetrics {
        ShiftsOpen = 100,
        ShiftsCompleted = 101,
        ActiveShiftCompletion = 102,
        AnomaliesReported = 103,
        AnomaliesResolved = 104,
        Movements = 105
    }

    //Interfaces
    interface IActivityChart {
        dateOfActivity: number;
        numberOfActivities: number;
    }

    interface IActivityMetrics {
        indicatorEnum: DashboardMetrics;
        value: number;
    }

    export class ActivityStats {
        private $assignedBarCart = $('#assignedBar');
        private $convertedBarChart = $('#convertedBar');
        private $convesionSpan = $('#conversionSpan');

        constructor(){
            this.getConvertionRatio();
        }

        private getConvertionRatio() {
            $.ajax({
                type: "POST",
                url: "../../Home/getMetricsforActivity",
                success: (json: Array<IActivityMetrics>) => {
                    var conversion: string = "";
                    var open: number = 0;
                    var closed: number = 0;

                    json.forEach(c=> {
                        switch (c.indicatorEnum) {
                            case DashboardMetrics.ShiftsCompleted:
                                closed = c.value;
                                break;
                            case DashboardMetrics.ShiftsOpen:
                                open = c.value;
                                break;
                            default: break;
                        }
                    });

                    //Set Values
                    this.$convesionSpan.html(closed.toString() + "/" + open.toString());
                    var persentage = Math.round((closed / (open + closed)) * 100);
                    this.$convertedBarChart.css('width', persentage.toString() + "%");
                }
            });
        }
    }

    export class ActivityChart {
        /* chart colors default */
        private $chrt_border_color: string = "#efefef";
        private $chrt_grid_color: string = "#DDD"
        private $chrt_main: string = "#6595b4";

        /* red       */
        private $chrt_second: string = "#6595b4";
        /* blue      */
        private $chrt_third: string = "#a5ce44";
        /* orange    */
        private $chrt_fourth: string = "#7e9d3a";
        /* green     */
        private $chrt_fifth: string = "#BD362F";
        /* dark red  */
        private $chrt_mono: string = "#000";

        /* Dom Elements */
        private $chart = $('#activitiesChart');
        private $loader = $('#activityChartLoader');

        /*Datasets for Plotting*/
        private shiftActivity: Array<Array<number>>;
        private anomalyActivity: Array<Array<number>>;

        constructor() {
            this.shiftActivity = [];
            this.anomalyActivity = [];
            this.getShiftActivities();
        }

        private getShiftActivities() {
            $.ajax({
                type: "POST",
                url: "../../Home/getActivities",
                success: (json: Array<IActivityChart>) => {
                    json.forEach(c => {
                        var plotData: Array<number> = [];
                        plotData.push(c.dateOfActivity);
                        plotData.push(c.numberOfActivities);
                        this.shiftActivity.push(plotData);
                    });

                    for (var i: number = 0; i < this.shiftActivity.length; ++i)
                        this.shiftActivity[i][0] += 60 * 60 * 1000;

                    this.getAnomalyActivities();
                }
            });
        }

        private getAnomalyActivities() {
            $.ajax({
                type: "POST",
                url: "../../Home/getAnomalies",
                success: (json: Array<IActivityChart>) => {
                    json.forEach(c => {
                        var plotData: Array<number> = [];
                        plotData.push(c.dateOfActivity);
                        plotData.push(c.numberOfActivities);
                        this.anomalyActivity.push(plotData);
                    });

                    for (var i: number = 0; i < this.anomalyActivity.length; ++i)
                        this.anomalyActivity[i][0] += 60 * 60 * 1000;

                    this.plotChart();
                }
            });
        }

        private plotChart() {

            if (this.$chart.length) {

                function weekendAreas(axes) {
                    var markings = [];
                    var d = new Date(axes.xaxis.min);

                    // go to the first Saturday
                    d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7))
                    d.setUTCSeconds(0);
                    d.setUTCMinutes(0);
                    d.setUTCHours(0);
                    var i = d.getTime();
                    do {
                        // when we don't set yaxis, the rectangle automatically
                        // extends to infinity upwards and downwards
                        markings.push({
                            xaxis: {
                                from: i,
                                to: i + 2 * 24 * 60 * 60 * 1000
                            }
                        });
                        i += 7 * 24 * 60 * 60 * 1000;
                    } while (i < axes.xaxis.max);

                    return markings;
                }

                var options = {
                    xaxis: {
                        mode: "time",
                    },
                    series: {
                        lines: {
                            show: true,
                            lineWidth: 1,
                            fill: true,
                            fillColor: {
                                colors: [{
                                    opacity: 0.1
                                }, {
                                        opacity: 0.15
                                    }]
                            }
                        },
                        points: { show: true },
                        shadowSize: 0
                    },
                    selection: {
                        mode: "x"
                    },
                    grid: {
                        hoverable: true,
                        clickable: true,
                        tickColor: this.$chrt_border_color,
                        borderWidth: 0,
                        borderColor: this.$chrt_border_color,
                    },
                    tooltip: true,
                    tooltipOpts: {
                        content: "%s data count: <span><b>%y</b></span>",
                        dateFormat: "%y-%0m-%0d",
                        defaultTheme: false
                    },
                    colors: [this.$chrt_main, this.$chrt_third],

                };

                var plot = $.plot(this.$chart,
                    [
                        {
                            data: this.shiftActivity,
                            label: "Shifts"
                        },
                        {
                            data: this.anomalyActivity,
                            label: "Anomalies"
                        }
                    ], options);
                this.$loader.fadeOut(300);
            };
        }

    }

} 

var activityChart = new AirSide.HomeHelper.ActivityChart();

var activityStats = new AirSide.HomeHelper.ActivityStats();