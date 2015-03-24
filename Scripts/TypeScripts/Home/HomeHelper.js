/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/flot/jquery.flot.d.ts" />
/// <reference path="../../typings/jqueryui/jqueryui.d.ts" />
/// <reference path="../../typings/jquery/jquery.bridge.d.ts" />
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
            DashboardMetrics[DashboardMetrics["FaultyLights"] = 105] = "FaultyLights";
            DashboardMetrics[DashboardMetrics["FaultyLightsResolved"] = 106] = "FaultyLightsResolved";
        })(DashboardMetrics || (DashboardMetrics = {}));
        var ActivityStats = (function () {
            function ActivityStats() {
                this.$convertedBarChart = $('#convertedBar');
                this.$convesionSpan = $('#conversionSpan');
                this.$faultyLightsSpan = $('#faultyLightsSpan');
                this.$faultyLightsBar = $('#faultyLightsBar');
            }
            ActivityStats.prototype.init = function () {
                this.getMetrics();
            };
            ActivityStats.prototype.getMetrics = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getMetricsforActivity",
                    success: function (json) {
                        var conversion = "";
                        var open = 0;
                        var closed = 0;
                        var faulty = 0;
                        var resolved = 0;
                        json.forEach(function (c) {
                            switch (c.indicatorEnum) {
                                case 101 /* ShiftsCompleted */:
                                    closed = c.value;
                                    break;
                                case 100 /* ShiftsOpen */:
                                    open = c.value;
                                    break;
                                case 105 /* FaultyLights */:
                                    faulty = c.value;
                                    break;
                                case 106 /* FaultyLightsResolved */:
                                    resolved = c.value;
                                    break;
                                default: break;
                            }
                        });
                        //Set Values for Shift
                        _this.$convesionSpan.html(closed.toString() + "/" + open.toString());
                        var persentage = Math.round((closed / (open + closed)) * 100);
                        _this.$convertedBarChart.css('width', persentage.toString() + "%");
                        //Set Values for Faulty Lights
                        _this.$faultyLightsSpan.html(resolved.toString() + "/" + faulty.toString());
                        persentage = Math.round((resolved / (faulty + resolved)) * 100);
                        _this.$faultyLightsBar.css('width', persentage.toString() + "%");
                    }
                });
            };
            ActivityStats.prototype.setTotalShiftCompletion = function (completed, totalAssets) {
                var $shiftCompletionSpan = $('#shiftCompletionSpan');
                var $shiftCompletionBar = $('#shiftCompletionBar');
                var persentage = Math.round((completed / totalAssets) * 100);
                $shiftCompletionSpan.html(persentage.toString() + "%");
                $shiftCompletionBar.css('width', persentage.toString() + '%');
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
            }
            ActivityChart.prototype.init = function () {
                this.getShiftActivities();
            };
            ActivityChart.prototype.getShiftActivities = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getActivities",
                    success: function (json) {
                        console.log(json);
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
            ActivityChart.prototype.setNumberOfAnomaliesReported = function (numberOfAnomalies) {
                var $anomaliesReported = $('#anomalyBar');
                var $anomalySpan = $('#anomalySpan');
                //TODO: Make the persentage relevant to closed anomalies
                var total = Math.round(numberOfAnomalies * 1.3);
                var persentage = Math.round((numberOfAnomalies / total) * 100);
                $anomalySpan.html(numberOfAnomalies.toString() + " Anomalies");
                $anomaliesReported.css('width', persentage.toString() + '%');
            };
            ActivityChart.prototype.getAnomalyActivities = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getAnomalies",
                    success: function (json) {
                        var numberOfAnomalies = 0;
                        json.forEach(function (c) {
                            var plotData = [];
                            plotData.push(c.dateOfActivity);
                            plotData.push(c.numberOfActivities);
                            numberOfAnomalies += c.numberOfActivities;
                            _this.anomalyActivity.push(plotData);
                        });
                        for (var i = 0; i < _this.anomalyActivity.length; ++i)
                            _this.anomalyActivity[i][0] += 60 * 60 * 1000;
                        _this.plotChart();
                        _this.setNumberOfAnomaliesReported(numberOfAnomalies);
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
        var ToDo = (function () {
            function ToDo() {
                this.todoCategoriesTxt = "";
                this._antiforgeryToken = $("input[name='__RequestVerificationToken']").val();
            }
            ToDo.prototype.init = function () {
                this.getTodoCategories();
            };
            ToDo.prototype.getTodoCategories = function () {
                var _this = this;
                $('#todoDiv').html('');
                $.ajax({
                    type: "POST",
                    url: "../../Home/getTodoCategories",
                    success: function (json) {
                        var allCategories;
                        $.each(json, function (i, v) {
                            var html = '<h5 class="todo-group-title"><i class="fa ' + v.vc_icon + '"></i> ' + v.vc_description + ' (<small class="num-of-tasks">0</small>)</h5>';
                            html += '<ul id="cat_' + v.i_todoCatId + '" class="todo"></ul>';
                            $('#todoDiv').append(html);
                            //Init Sorting
                            $('#cat_' + v.i_todoCatId).sortable({
                                handle: '.handle',
                                connectWith: ".todo",
                                update: _this.countTasks()
                            }).disableSelection();
                            _this.todoCategoriesTxt += '[' + v.vc_description + ']';
                        });
                        //Init Completed Tasks
                        var html = '<h5 class="todo-group-title"><i class="fa fa-check"></i> Completed Tasks (<small class="num-of-tasks">0</small>)</h5><ul id="sortable3" class="todo"></ul>';
                        $('#todoDiv').append(html);
                        //Show ToDo
                        $('#todoLoader').hide();
                        $('#todoDiv').fadeIn(500);
                        $("#todoAddBtn").removeClass('disabled');
                        _this.getAllToDos();
                        // initialize sortable
                        $(function (c) {
                            $(allCategories).sortable({
                                handle: '.handle',
                                connectWith: ".todo",
                                update: _this.countTasks()
                            }).disableSelection();
                        });
                    }
                });
            };
            ToDo.prototype.getAllToDos = function () {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/getAllTodos",
                    success: function (json) {
                        $.each(json, function (i, v) {
                            _this.insertTodoItem(v);
                        });
                        _this.bindToDo();
                        _this.countTasks();
                    }
                });
            };
            // count tasks
            ToDo.prototype.countTasks = function () {
                var $todoTitle = $('.todo-group-title');
                $todoTitle.each(function (i, v) {
                    var $this = $(v);
                    $this.find(".num-of-tasks").text($this.next().find("li").length);
                });
            };
            ToDo.prototype.setTodoStatus = function (id) {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/setToDoStatus?todoId=" + id,
                    data: { __RequestVerificationToken: this._antiforgeryToken },
                    success: function (json) {
                        _this.getTodoCategories();
                    }
                });
            };
            ToDo.prototype.insertTodoItem = function (json) {
                if (json.bt_active === true) {
                    var html = '<li data-todoid="' + json.i_todoProfileId + '"><span class="handle"><label class="checkbox"><input type="checkbox" name="checkbox-inline"><i></i></label></span>';
                    html += '<p><strong>Item #' + json.i_todoProfileId + '</strong> - ' + json.vc_description + ' <span class="text-muted"></span>';
                    html += '<span class="date">' + json.date + '</span></p></li>';
                    $('#cat_' + json.i_todoCatId).append(html);
                }
                else {
                    var html = '<li class="complete"><span class="handle" style="display:none"><label class="checkbox state-disabled"><input type="checkbox" name="checkbox-inline" disabled="disabled"><i></i></label></span>';
                    html += '<p><strong>Item #' + json.i_todoProfileId + '</strong> - ' + json.vc_description + ' <span class="text-muted"></span>';
                    html += '<span class="date">' + json.date + '</span></p></li>';
                    $('#sortable3').append(html);
                }
            };
            ToDo.prototype.addTodo = function (desc, cat) {
                var _this = this;
                $.ajax({
                    type: "POST",
                    url: "../../Home/insertNewTodo?description=" + desc + "&category=" + cat,
                    data: { __RequestVerificationToken: this._antiforgeryToken },
                    success: function (json) {
                        var alert = '<p>' + json.vc_description + '</p><small>Date ' + json.date + '</small>';
                        $.bigBox({
                            title: "To-Do Item Created",
                            content: alert,
                            color: "#3276B1",
                            timeout: 5000,
                            icon: "fa fa-bell swing animated",
                        });
                        _this.insertTodoItem(json);
                        _this.bindToDo();
                        _this.countTasks();
                    }
                });
            };
            ToDo.prototype.bindToDo = function () {
                var _this = this;
                // check and uncheck
                var $main = $('.todo .checkbox > input[type="checkbox"]');
                $main.click(function (c) {
                    var $this = $main.parent().parent().parent();
                    var todoId = $this.attr("data-todoid");
                    if ($main.prop('checked')) {
                        $this.addClass("complete");
                        // remove this if you want to undo a check list once checked
                        $main.attr("disabled", "true");
                        $main.parent().hide();
                        //update server
                        _this.setTodoStatus(todoId);
                    }
                    else {
                    }
                });
            };
            return ToDo;
        })();
        HomeHelper.ToDo = ToDo;
    })(HomeHelper = AirSide.HomeHelper || (AirSide.HomeHelper = {}));
})(AirSide || (AirSide = {}));
var activityChart = new AirSide.HomeHelper.ActivityChart();
var activityStats = new AirSide.HomeHelper.ActivityStats();
var todos = new AirSide.HomeHelper.ToDo();
$(document).on('ready', function (c) {
    todos.init();
    activityChart.init();
    activityStats.init();
});
//init button clicks
$(document).on('click', '#todoAddBtn', function () {
    var itemDesc, itemCat;
    $.SmartMessageBox({
        title: '<i class="fa fa-check fa-lg txt-color-blue"></i> New To-Do Item',
        content: "Please enter the item description",
        buttons: "[Cancel][Accept]",
        input: "text",
        inputValue: "",
        placeholder: "Enter your to-do item"
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Cancel") {
            return 0;
        }
        itemDesc = Value;
        $.SmartMessageBox({
            title: '<i class="fa fa-list fa-lg txt-color-blue"></i> Select To-Do Category',
            content: "Please select a category for the new item",
            buttons: "[Done]",
            input: "select",
            options: todos.todoCategoriesTxt,
        }, function (ButtonPress, Value) {
            itemCat = Value;
            todos.addTodo(itemDesc, itemCat);
        });
    });
});
//# sourceMappingURL=HomeHelper.js.map