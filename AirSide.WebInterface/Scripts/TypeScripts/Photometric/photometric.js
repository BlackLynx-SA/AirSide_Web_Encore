var AirSide;
(function (AirSide) {
    var Encore;
    (function (Encore) {
        var Photometric;
        (function (Photometric) {
            var PhotometricReports = (function () {
                function PhotometricReports() {
                    this.photometricData = [];
                    this.photoDates = [];
                    var threshold = localStorage["_threshold"];
                    if (threshold === undefined)
                        localStorage["_threshold"] = 0.5;
                    this.threshold = localStorage["_threshold"];
                    var knobValue = localStorage["_threshold"] * 100;
                    $('.knob').attr("value", knobValue);
                    var ctx = $("#bar-chart");
                    this.chart = new Chart(ctx, {
                        type: 'bar',
                        data: {
                            labels: [],
                            datasets: []
                        }
                    });
                }
                PhotometricReports.prototype.getAvailableDates = function () {
                    var $this = this;
                    $.getJSON("GetAvailableDates", function (data, text, jq) {
                        $this.photoDates = data;
                        $this.buildDateSelect();
                        console.log(data);
                    });
                };
                PhotometricReports.prototype.buildDateSelect = function () {
                    var html = '<option></option>';
                    this.photoDates.forEach(function (c) {
                        html += '<option>' + c + '</option>';
                    });
                    $('#dateSelect').html(html);
                };
                PhotometricReports.prototype.getLabels = function () {
                    var returnLabels = [];
                    this.photometricData.forEach(function (c) {
                        returnLabels.push(c.SerialNumber);
                    });
                    return returnLabels;
                };
                PhotometricReports.prototype.getMaxIntensity = function () {
                    var $this = this;
                    var data = [];
                    var backgroundColor = [];
                    var borderColor = [];
                    var checked = $('#failedLightsCheck').prop('checked');
                    this.photometricData.forEach(function (c) {
                        //Calculate Threashold
                        var percentage = c.MaxIntensity / 5000;
                        if (percentage < $this.threshold) {
                            backgroundColor.push('rgba(255, 0, 0, 0.2)');
                            borderColor.push('rgba(255, 151, 151, 1)');
                            data.push(c.MaxIntensity);
                        }
                        else if (checked === false) {
                            backgroundColor.push('rgba(40, 204, 118, 0.2)');
                            borderColor.push('rgba(132, 237, 139, 1)');
                            data.push(c.MaxIntensity);
                        }
                    });
                    var dataSet = {
                        label: "Max. Intensity",
                        data: data,
                        backgroundColor: backgroundColor,
                        borderColor: borderColor,
                        borderWidth: 1
                    };
                    return dataSet;
                };
                PhotometricReports.prototype.getAvgIntensity = function () {
                    var $this = this;
                    var checked = $('#failedLightsCheck').prop('checked');
                    var data = [];
                    var backgroundColor = [];
                    var borderColor = [];
                    this.photometricData.forEach(function (c) {
                        //Calculate Threashold
                        var percentage = c.AvgIntensity / 5000;
                        if (percentage < $this.threshold) {
                            backgroundColor.push('rgba(255, 0, 0, 0.2)');
                            borderColor.push('rgba(255, 151, 151, 1)');
                            data.push(c.AvgIntensity);
                        }
                        else if (checked === false) {
                            backgroundColor.push('rgba(40, 204, 118, 0.2)');
                            borderColor.push('rgba(132, 237, 139, 1)');
                            data.push(c.AvgIntensity);
                        }
                    });
                    var dataSet = {
                        label: "Avg. Intensity",
                        type: "bar",
                        data: data,
                        backgroundColor: backgroundColor,
                        borderColor: borderColor,
                        borderWidth: 1
                    };
                    return dataSet;
                };
                PhotometricReports.prototype.getThresholdSet = function () {
                    var data = [];
                    var percentage = this.threshold * 5000;
                    for (var i = 0; i < this.photometricData.length; i++) {
                        data.push(percentage);
                    }
                    var dataSet = {
                        label: "Threshold",
                        type: "line",
                        data: data,
                        borderColor: 'rgba(63, 158, 255, 1)',
                        backgroundColor: 'rgba(140, 195, 255, 0.1)',
                        borderWidth: 1
                    };
                    return dataSet;
                };
                PhotometricReports.prototype.buildChart = function () {
                    var $this = this;
                    this.chart.destroy();
                    var labels = this.getLabels();
                    var ctx = $("#bar-chart");
                    var dataSets = [];
                    var maxSet = this.getMaxIntensity();
                    var avgSet = this.getAvgIntensity();
                    var thresholdSet = this.getThresholdSet();
                    dataSets.push(maxSet);
                    dataSets.push(avgSet);
                    dataSets.push(thresholdSet);
                    this.chart = new Chart(ctx, {
                        type: 'bar',
                        data: {
                            labels: labels,
                            datasets: dataSets
                        },
                        options: {
                            scales: {
                                yAxes: [
                                    {
                                        ticks: {
                                            beginAtZero: true
                                        }
                                    }
                                ]
                            },
                            onClick: function (c) {
                                var index = $this.chart.getElementsAtEvent(c);
                                $this.getPicture(index[0]._model.label);
                            },
                            legend: {
                                display: false
                            }
                        }
                    });
                };
                PhotometricReports.prototype.getPicture = function (label) {
                    var html = '<h3>Isocandela Diagram for ' + label + '</h3>';
                    $('#pictureDiv').hide();
                    this.photometricData.forEach(function (c) {
                        if (c.SerialNumber === label) {
                            html += '<img src="' + c.PictureUrl + '" />';
                        }
                    });
                    $('#pictureDiv').html(html);
                    $('#pictureDiv').fadeIn(300);
                };
                PhotometricReports.prototype.getPhotometricData = function (date) {
                    var $this = this;
                    var data = { selectedDate: date };
                    $.ajax({
                        url: 'GetPhotometricData',
                        type: 'post',
                        data: data,
                        dataType: 'json',
                        success: function (json) {
                            $this.photometricData = json;
                            $this.buildChart();
                        }
                    });
                };
                return PhotometricReports;
            }());
            Photometric.PhotometricReports = PhotometricReports;
        })(Photometric = Encore.Photometric || (Encore.Photometric = {}));
    })(Encore = AirSide.Encore || (AirSide.Encore = {}));
})(AirSide || (AirSide = {}));
$(document).ready(function () {
    var photometric = new AirSide.Encore.Photometric.PhotometricReports();
    photometric.getAvailableDates();
    $(document).on('change', '#dateSelect', function (c) {
        photometric.getPhotometricData($('#dateSelect').val());
    });
    $(document).on('click', '#failedLightsCheck', function (c) {
        photometric.buildChart();
    });
    $(document).on('click', '#redrawBtn', function (c) {
        photometric.buildChart();
    });
    $(window).resize(function (c) {
        photometric.chart.resize();
    });
    $('.knob').knob({
        change: function (value) {
            localStorage["_threshold"] = value / 100;
            photometric.threshold = value / 100;
        },
        release: function (value) {
            //console.log(this.$.attr('value'));
            //console.log("release : " + value);
        },
        cancel: function () {
            //console.log("cancel : ", this);
        }
    });
    $('.knob').fadeIn(300);
});
