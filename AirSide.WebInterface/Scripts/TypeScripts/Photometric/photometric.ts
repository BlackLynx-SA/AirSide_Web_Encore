module AirSide.Encore.Photometric {
    export class PhotometricReports {
        photometricData: Array<IPhotometricViewModel>;
        photoDates: Array<string>;

        constructor() {
            this.photometricData = [];
            this.photoDates = [];
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

        buildChart() {
            Morris.Bar({
                element: 'bar-chart',
                data: this.photometricData,
                xkey: 'SerialNumber',
                ykeys: ['MaxIntensity', 'AvgIntensity'],
                labels: ['Max', 'Avg']
            });
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
});