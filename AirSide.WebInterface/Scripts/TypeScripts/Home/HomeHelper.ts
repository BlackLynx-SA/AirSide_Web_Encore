/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/flot/jquery.flot.d.ts" />
/// <reference path="../../typings/jqueryui/jqueryui.d.ts" />
/// <reference path="../../typings/jquery/jquery.bridge.d.ts" />

module AirSide.HomeHelper {


    //Enum
    enum DashboardMetrics {
        ShiftsOpen = 100,
        ShiftsCompleted = 101,
        ActiveShiftCompletion = 102,
        AnomaliesReported = 103,
        AnomaliesResolved = 104,
        FaultyLights = 105,
        FaultyLightsResolved = 106
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

    interface IToDoCategory {
        i_todoCatId: number;
        vc_description: string;
        bt_private: boolean;
        UserId: number;
        vc_icon: string;
    }

    interface IToDoItem {
        date: string;
        vc_description: string;
        i_todoProfileId: number;
        i_todoCatId: number;
        bt_active: boolean;
    }

    export class ActivityStats {
        private $convertedBarChart = $('#convertedBar');
        private $convesionSpan = $('#conversionSpan');
        private $faultyLightsSpan = $('#faultyLightsSpan');
        private $faultyLightsBar = $('#faultyLightsBar');

        constructor(){}

        init() {
            this.getMetrics();
        }

        private getMetrics() {
            $.ajax({
                type: "POST",
                url: "../../Home/getMetricsforActivity",
                success: (json: Array<IActivityMetrics>) => {
                    var conversion: string = "";
                    var open: number = 0;
                    var closed: number = 0;
                    var faulty: number = 0;
                    var resolved: number = 0;

                    json.forEach(c=> {
                        switch (c.indicatorEnum) {
                            case DashboardMetrics.ShiftsCompleted:
                                closed = c.value;
                                break;
                            case DashboardMetrics.ShiftsOpen:
                                open = c.value;
                                break;
                            case DashboardMetrics.FaultyLights:
                                faulty = c.value;
                                break;
                            case DashboardMetrics.FaultyLightsResolved:
                                resolved = c.value;
                                break;
                            default: break;
                        }
                    });

                    //Set Values for Shift
                    this.$convesionSpan.html(closed.toString() + "/" + open.toString());
                    var persentage = Math.round((closed / (open + closed)) * 100);
                    this.$convertedBarChart.css('width', persentage.toString() + "%");

                    //Set Values for Faulty Lights
                    this.$faultyLightsSpan.html(resolved.toString() + "/" + faulty.toString());
                    persentage = Math.round((resolved / (faulty + resolved)) * 100);
                    this.$faultyLightsBar.css('width', persentage.toString() + "%");
                }
            });
        }

        setTotalShiftCompletion(completed: number, totalAssets: number) {
            var $shiftCompletionSpan = $('#shiftCompletionSpan');
            var $shiftCompletionBar = $('#shiftCompletionBar');

            var persentage = Math.round((completed / totalAssets) * 100);
            $shiftCompletionSpan.html(persentage.toString() + "%");
            $shiftCompletionBar.css('width', persentage.toString() + '%');
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
        }

        init() {
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

        private setNumberOfAnomaliesReported(numberOfAnomalies: number) {
            var $anomaliesReported = $('#anomalyBar');
            var $anomalySpan = $('#anomalySpan');

            //TODO: Make the persentage relevant to closed anomalies
            var total: number = Math.round(numberOfAnomalies * 1.3);
            var persentage: number = Math.round((numberOfAnomalies / total) * 100);

            $anomalySpan.html(numberOfAnomalies.toString() + " Anomalies");
            $anomaliesReported.css('width', persentage.toString() + '%');
        }

        private getAnomalyActivities() {
            $.ajax({
                type: "POST",
                url: "../../Home/getAnomalies",
                success: (json: Array<IActivityChart>) => {
                    var numberOfAnomalies: number = 0;
                    json.forEach(c => {
                        var plotData: Array<number> = [];
                        plotData.push(c.dateOfActivity);
                        plotData.push(c.numberOfActivities);
                        numberOfAnomalies += c.numberOfActivities;
                        this.anomalyActivity.push(plotData);
                    });

                    for (var i: number = 0; i < this.anomalyActivity.length; ++i)
                        this.anomalyActivity[i][0] += 60 * 60 * 1000;

                    this.plotChart();

                    this.setNumberOfAnomaliesReported(numberOfAnomalies);
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

    export class ToDo {
                
        todoCategoriesTxt: string;
        private _antiforgeryToken: string;

        constructor() {
            this.todoCategoriesTxt = "";
            this._antiforgeryToken = $("input[name='__RequestVerificationToken']").val();
        }

        init() {
            this.getTodoCategories();
        }

        private getTodoCategories() {
            $('#todoDiv').html('');
            $.ajax({
                type: "POST",
                url: "../../Home/getTodoCategories",
                success: (json: Array<IToDoCategory>) => {
                    var allCategories;
                    $.each(json, (i, v) => {
                        var html: string = '<h5 class="todo-group-title"><i class="fa ' + v.vc_icon + '"></i> ' + v.vc_description + ' (<small class="num-of-tasks">0</small>)</h5>';
                        html += '<ul id="cat_' + v.i_todoCatId + '" class="todo"></ul>';
                        $('#todoDiv').append(html);

                        //Init Sorting
                        $('#cat_' + v.i_todoCatId).sortable({
                            handle: '.handle',
                            connectWith: ".todo",
                            update: this.countTasks()
                        }).disableSelection();

                        this.todoCategoriesTxt += '[' + v.vc_description + ']';
                    });

                    //Init Completed Tasks
                    var html: string = '<h5 class="todo-group-title"><i class="fa fa-check"></i> Completed Tasks (<small class="num-of-tasks">0</small>)</h5><ul id="sortable3" class="todo"></ul>';
                    $('#todoDiv').append(html);

                    //Show ToDo
                    $('#todoLoader').hide();
                    $('#todoDiv').fadeIn(500);
                    $("#todoAddBtn").removeClass('disabled');
                    this.getAllToDos();

                    // initialize sortable
                    $(c=> {
                        $(allCategories).sortable({
                            handle: '.handle',
                            connectWith: ".todo",
                            update: this.countTasks()
                        }).disableSelection();
                    });
                }
            });
        }

        private getAllToDos() {
        $.ajax({
            type: "POST",
            url: "../../Home/getAllTodos",
            success: (json: Array<IToDoItem>) => {
                $.each(json,(i: number, v: IToDoItem) => {
                    this.insertTodoItem(v);
                });
                this.bindToDo();
                this.countTasks();
                }
            });
        }

        // count tasks
        private countTasks() {
            var $todoTitle = $('.todo-group-title');

            $todoTitle.each((i: number, v: any) => {
                var $this = $(v);
                $this.find(".num-of-tasks").text($this.next().find("li").length);
            });
        }

        private setTodoStatus(id) {
            $.ajax({
                type: "POST",
                url: "../../Home/setToDoStatus?todoId=" + id,
                data: { __RequestVerificationToken: this._antiforgeryToken },
                success: (json) => {
                    this.getTodoCategories();
                }
            });
        }

        private insertTodoItem(json: IToDoItem) {
            if (json.bt_active === true) {
                var html: string = '<li data-todoid="' + json.i_todoProfileId + '"><span class="handle"><label class="checkbox"><input type="checkbox" name="checkbox-inline"><i></i></label></span>';
                html += '<p><strong>Item #' + json.i_todoProfileId + '</strong> - ' + json.vc_description + ' <span class="text-muted"></span>';
                html += '<span class="date">' + json.date + '</span></p></li>';
                $('#cat_' + json.i_todoCatId).append(html);
            } else {
                var html: string = '<li class="complete"><span class="handle" style="display:none"><label class="checkbox state-disabled"><input type="checkbox" name="checkbox-inline" disabled="disabled"><i></i></label></span>';
                html += '<p><strong>Item #' + json.i_todoProfileId + '</strong> - ' + json.vc_description + ' <span class="text-muted"></span>';
                html += '<span class="date">' + json.date + '</span></p></li>';
                $('#sortable3').append(html);
            }
        }

        addTodo(desc, cat) {
            $.ajax({
                type: "POST",
                url: "../../Home/insertNewTodo?description=" + desc + "&category=" + cat,
                data: { __RequestVerificationToken: this._antiforgeryToken },
                success: (json: IToDoItem) => {
                    var alert:string = '<p>' + json.vc_description + '</p><small>Date ' + json.date + '</small>';
                    $.bigBox({
                        title: "To-Do Item Created",
                        content: alert,
                        color: "#3276B1",
                        timeout: 5000,
                        icon: "fa fa-bell swing animated",
                    });
                    this.insertTodoItem(json);
                    this.bindToDo();
                    this.countTasks();
                }
            });
        }

        private bindToDo() {
            // check and uncheck
            var $main = $('.todo .checkbox > input[type="checkbox"]');
            $main.click(c=> {
                var $this = $main.parent().parent().parent();
                var todoId = $this.attr("data-todoid");

                if ($main.prop('checked')) {
                    $this.addClass("complete");

                    // remove this if you want to undo a check list once checked
                    $main.attr("disabled", "true");
                    $main.parent().hide();

                    //update server
                    this.setTodoStatus(todoId);

                    //// once clicked - add class, copy to memory then remove and add to sortable3
                    //$this.slideUp(500, function () {
                    //    $this.clone().prependTo("#sortable3").effect("highlight", {}, 800);
                    //    $this.remove();
                        
                    //});

                    //this.countTasks();
                } else {
                    // insert undo code here...
                }

            });
        }
        }
} 

var activityChart = new AirSide.HomeHelper.ActivityChart();

var activityStats = new AirSide.HomeHelper.ActivityStats();

$(document).on('ready', c=> {
    activityChart.init();
    activityStats.init();

    //Check for new release notes
    var release: string = localStorage.getItem('releasenotes.20160528');
    if (release === null)
        $('#releaseNotesModal').modal('show');
});

$(document).on('click', '#ReleaseOkBtn', c => {
    $('#releaseNotesModal').modal('hide');
    localStorage.setItem('releasenotes.20160528', 'true');
});
