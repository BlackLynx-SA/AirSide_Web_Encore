module AirSide.Encore.Photometric {
    export class PhotometricReports {
        photometricData: Array<IPhotometricViewModel>;
        photoDates: Array<string>;
        threshold: number;
        chart: Chart;

        constructor() {
            this.photometricData = [];
            this.photoDates = [];
            var threshold = localStorage["_threshold"];
            if (threshold === undefined)
                localStorage["_threshold"] = 0.5;
            this.threshold = localStorage["_threshold"];
            var knobValue = localStorage["_threshold"] * 100;
            $('.knob').attr("value", knobValue);

            var ctx = $("#bar-chart");
            this.chart = new Chart(ctx,
            {
                type: 'bar',
                data: {
                    labels: [],
                    datasets: []
                }
            });
        }

        getAvailableDates() {
            var $this = this;
            $.getJSON("GetAvailableDates", (data: Array<string>, text: string, jq: JQueryXHR) => {
                $this.photoDates = data;
                $this.buildDateSelect();
                console.log(data);
            });
        }

        buildDateSelect() {
            var html: string = '<option></option>';
            this.photoDates.forEach(c => {
                html += '<option>' + c + '</option>';
            });

            $('#dateSelect').html(html);
        }

        private getLabels(): Array<string> {
            var returnLabels: Array<string> = [];
            this.photometricData.forEach(c => {
                returnLabels.push(c.SerialNumber);
            });
            return returnLabels;
        }

        private getMaxIntensity() {
            var $this = this;
            var data: Array<number> = [];
            var backgroundColor: Array<string> = [];
            var borderColor: Array<string> = [];
            var checked = $('#failedLightsCheck').prop('checked');

            this.photometricData.forEach(c => {

                //Calculate Threashold
                var percentage = c.MaxIntensity / 5000;
                if (percentage < $this.threshold) {
                    backgroundColor.push('rgba(255, 0, 0, 0.2)');
                    borderColor.push('rgba(255, 151, 151, 1)');
                    data.push(c.MaxIntensity);
                } else if (checked === false) {
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
            }

            return dataSet;
        }

        private getAvgIntensity() {
            var $this = this;
            var checked = $('#failedLightsCheck').prop('checked');
            var data: Array<number> = [];
            var backgroundColor: Array<string> = [];
            var borderColor: Array<string> = [];

            this.photometricData.forEach(c => {

                //Calculate Threashold
                var percentage = c.AvgIntensity / 5000;
                if (percentage < $this.threshold) {
                    backgroundColor.push('rgba(255, 0, 0, 0.2)');
                    borderColor.push('rgba(255, 151, 151, 1)');
                    data.push(c.AvgIntensity);
                } else if(checked === false){
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
            }

            return dataSet;
        }

        private getThresholdSet() {
            var data: Array<number> = [];
            var percentage: number = this.threshold * 5000;
            for (var i: number = 0; i < this.photometricData.length; i++) {
                data.push(percentage);

            }

            var dataSet = {
                label: "Threshold",
                type: "line",
                data: data,
                borderColor: 'rgba(63, 158, 255, 1)',
                backgroundColor: 'rgba(140, 195, 255, 0.1)',
                borderWidth: 1
            }

            return dataSet;
        }

        buildChart() {
            var $this = this;
            this.chart.destroy();
            var labels: Array<string> = this.getLabels();
            var ctx = $("#bar-chart");
            var dataSets: Array<any> = [];
            var maxSet = this.getMaxIntensity();
            var avgSet = this.getAvgIntensity();
            var thresholdSet = this.getThresholdSet();

            dataSets.push(maxSet);
            dataSets.push(avgSet);
            dataSets.push(thresholdSet);

            this.chart = new Chart(ctx,
            {
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
                    onClick: c => {
                        var index = $this.chart.getElementsAtEvent(c);
                        $this.getPicture(index[0]._model.label);
                    },
                    legend: {
                        display: false
                    }
                }
            });
        }

        private getPicture(label: string) {
            var html: string = '<h3>Isocandela Diagram for ' + label + '</h3>';
            $('#pictureDiv').hide();
            this.photometricData.forEach(c => {
                if (c.SerialNumber === label) {
                    html += '<img src="' + c.PictureUrl + '" />';
                }
            });

            $('#pictureDiv').html(html);
            $('#pictureDiv').fadeIn(300);
        }

        getPhotometricData(date: string) {
            var $this = this;
            var data = { selectedDate: date };
            $.ajax({
                url: 'GetPhotometricData',
                type: 'post',
                data: data,
                dataType: 'json',
                success: (json: Array<IPhotometricViewModel>) => {
                    $this.photometricData = json;
                    $this.buildChart();
                }
            });
        }
    }
}

$(document).ready(():void => {
    var photometric = new AirSide.Encore.Photometric.PhotometricReports();

    photometric.getAvailableDates();

    $(document).on('change', '#dateSelect', c => {
        photometric.getPhotometricData($('#dateSelect').val());
    });

    $(document).on('click', '#failedLightsCheck', c => {
        photometric.buildChart();
    });

    $(document).on('click', '#redrawBtn', c => {
        photometric.buildChart();
    });

    $(window).resize(c => {
        photometric.chart.resize();
    });

    $('.knob').knob({
        change: value => {
            localStorage["_threshold"] = value / 100;
            photometric.threshold = value / 100;
        },
        release: value => {
            //console.log(this.$.attr('value'));
            //console.log("release : " + value);
        },
        cancel: ():void => {
            //console.log("cancel : ", this);
        }
    });

    $('.knob').fadeIn(300);
});