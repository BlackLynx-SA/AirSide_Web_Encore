/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/flot/jquery.flot.d.ts" />
var AirSide;
(function (AirSide) {
    var HomeHelper;
    (function (HomeHelper) {
        //Enum
        var DashboardMetrics;
        (function (DashboardMetrics) {
            DashboardMetrics[DashboardMetrics["ShiftsOpen"] = 100] = "ShiftsOpen";
            DashboardMetrics[DashboardMetrics["ShiftsCompleted"] = 101] = "ShiftsCompleted";
            DashboardMetrics[DashboardMetrics["ActiveShiftCompletion"] = 102] = "ActiveShiftCompletion";
            DashboardMetrics[DashboardMetrics["AnomaliesReported"] = 103] = "AnomaliesReported";
            DashboardMetrics[DashboardMetrics["AnomaliesResolved"] = 104] = "AnomaliesResolved";
            DashboardMetrics[DashboardMetrics["Movements"] = 105] = "Movements";
        })(DashboardMetrics || (DashboardMetrics = {}));
        var ActivityStats = (function () {
            function ActivityStats() {
                this.$assignedBarCart = $('#assignedBar');
                this.$convertedBarChart = $('#convertedBar');
                this.$convesionSpan = $('#conversionSpan');
                this.getConvertionRatio();
            }
            ActivityStats.prototype.getConvertionRatio = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getMetricsforActivity",
                    success: function (json) {
                        var conversion = "";
                        var open = 0;
                        var closed = 0;
                        json.forEach(function (c) {
                            switch (c.indicatorEnum) {
                                case 101 /* ShiftsCompleted */:
                                    closed = c.value;
                                    break;
                                case 100 /* ShiftsOpen */:
                                    open = c.value;
                                    break;
                                default: break;
                            }
                        });
                        //Set Values
                        _this.$convesionSpan.html(closed.toString() + "/" + open.toString());
                        var persentage = Math.round((closed / (open + closed)) * 100);
                        _this.$convertedBarChart.css('width', persentage.toString() + "%");
                    }
                });
            };
            return ActivityStats;
        })();
        HomeHelper.ActivityStats = ActivityStats;
        var ActivityChart = (function () {
            function ActivityChart() {
                /* chart colors default */
                this.$chrt_border_color = "#efefef";
                this.$chrt_grid_color = "#DDD";
                this.$chrt_main = "#6595b4";
                /* red       */
                this.$chrt_second = "#6595b4";
                /* blue      */
                this.$chrt_third = "#a5ce44";
                /* orange    */
                this.$chrt_fourth = "#7e9d3a";
                /* green     */
                this.$chrt_fifth = "#BD362F";
                /* dark red  */
                this.$chrt_mono = "#000";
                /* Dom Elements */
                this.$chart = $('#activitiesChart');
                this.$loader = $('#activityChartLoader');
                this.shiftActivity = [];
                this.anomalyActivity = [];
                this.getShiftActivities();
            }
            ActivityChart.prototype.getShiftActivities = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getActivities",
                    success: function (json) {
                        json.forEach(function (c) {
                            var plotData = [];
                            plotData.push(c.dateOfActivity);
                            plotData.push(c.numberOfActivities);
                            _this.shiftActivity.push(plotData);
                        });
                        for (var i = 0; i < _this.shiftActivity.length; ++i)
                            _this.shiftActivity[i][0] += 60 * 60 * 1000;
                        _this.getAnomalyActivities();
                    }
                });
            };
            ActivityChart.prototype.getAnomalyActivities = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getAnomalies",
                    success: function (json) {
                        json.forEach(function (c) {
                            var plotData = [];
                            plotData.push(c.dateOfActivity);
                            plotData.push(c.numberOfActivities);
                            _this.anomalyActivity.push(plotData);
                        });
                        for (var i = 0; i < _this.anomalyActivity.length; ++i)
                            _this.anomalyActivity[i][0] += 60 * 60 * 1000;
                        _this.plotChart();
                    }
                });
            };
            ActivityChart.prototype.plotChart = function () {
                if (this.$chart.length) {
                    function weekendAreas(axes) {
                        var markings = [];
                        var d = new Date(axes.xaxis.min);
                        // go to the first Saturday
                        d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 1) % 7));
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
                    var plot = $.plot(this.$chart, [
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
                }
                ;
            };
            return ActivityChart;
        })();
        HomeHelper.ActivityChart = ActivityChart;
    })(HomeHelper = AirSide.HomeHelper || (AirSide.HomeHelper = {}));
})(AirSide || (AirSide = {}));
var activityChart = new AirSide.HomeHelper.ActivityChart();
var activityStats = new AirSide.HomeHelper.ActivityStats();
//# sourceMappingURL=HomeHelper.js.map