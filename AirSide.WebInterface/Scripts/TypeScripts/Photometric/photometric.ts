module AirSide.Encore.Photometric {
    export class PhotometricReports {
        photometricData: Array<IPhotometricViewModel>;
        dates: Array<string>;

        constructor() {
            this.photometricData = [];
            this.dates = [];
        }

        getAvailableDates() {
            var $this = this;
            $.getJSON("GetAvailableDates", (data: Array<string>, text: string, jq: JQueryXHR) => {
                $this.dates = data;
                console.log(data);
            });
        }
    }
}

$(document).ready(():void => {
    var photometric = new AirSide.Encore.Photometric.PhotometricReports();

    photometric.getAvailableDates();
});