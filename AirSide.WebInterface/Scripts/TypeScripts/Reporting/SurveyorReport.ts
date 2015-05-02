/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/pdfjs/pdfjs.d.ts" />
/// <reference path="../../typings/bootstrap.datepicker/bootstrap.datepicker.d.ts" />
/// <reference path="../../typings/jquery/jquery.bridge.d.ts" />

module AirSide.Reporting {
    interface ISurveyorData {
        guid: string;
        url: string;
        description: string;
        type: string;
        technician: string;
        date: string;
        longitude: number;
        latitude: number;
    }

    export class SurveyorData {

        $pictureTable = $('#pictureTbl tbody');
        $voiceTable = $('#voiceTbl tbody');
        $pageLoader = $('#page_loader');
        dateRange: string;
        surveyType: number;
        all: boolean;

        constructor() {
            this.dateRange = "";
            this.all = false;
            this.surveyType = 0;
            this.initDatePickers();
        }

        getData() {
            this.$pageLoader.fadeIn(300);
            $.ajax({
                type: "POST",
                url: "../../Surveyor/getSurveyorData?anomalyType=" + this.surveyType + "&dateRange=" + this.dateRange + "&All=" + this.all,
                success: (json: Array<ISurveyorData>) => {
                    console.log(json);
                    this.$pageLoader.hide();
                    var html: string = "";
                    json.forEach(c=> {
                        html += "<tr>";
                        var severity: string = "None";
                        switch (c.type) {
                            case "1": html += '<tr class="danger">';
                                severity = "Critical";
                                break;
                            case "2": html += '<tr class="warning">';
                                severity = "Major";
                                break;
                            case "3": html += '<tr class="info">';
                                severity = "Minor";
                                break;
                            default: break;
                        }
                        var url: string = c.url;
                        if(this.surveyType === 1)
                            html += '<td>' + c.date + '</td><td>' + c.technician + '</td><td>' + severity + '</td><td><a class="btn btn-success btn-sm surveyBtn" onclick="showPicture(\'' + url + '\', ' + c.longitude + ',' + c.latitude + ')"><i class="fa fa-camera"></i> View</a><a class="btn btn-danger btn-sm surveyBtn" onclick="closeAnomaly(\'' + c.guid + '\')"><i class="fa fa-times"></i> Close</a></td></tr>'
                        else if(this.surveyType === 2)
                            html += '<td>' + c.date + '</td><td>' + c.technician + '</td><td>' + severity + '</td><td><a class="btn btn-success btn-sm surveyBtn" onclick="showVoice(\'' + url + '\', ' + c.longitude + ',' + c.latitude + ')"><i class="fa fa-microphone"></i> Listen</a><a class="btn btn-danger btn-sm surveyBtn" onclick="closeAnomaly(\'' + c.guid + '\')"><i class="fa fa-times"></i> Close</a></td></tr>'
                    });

                    switch (this.surveyType) {
                        case 1: this.$pictureTable.html(html);
                            break;
                        case 2: this.$voiceTable.html(html);
                        default:
                            break;
                    }
                }
            });
        }

        closeAnomaly(guid: string) {
            this.$pageLoader.fadeIn(300);
            $.ajax({
                type: "POST",
                url: "../../Surveyor/closeAnomaly?guid=" + guid,
                success: (json: string) => {
                    this.getData();
                }
            });
        }

        private initDatePickers() {
            // Date Range Picker
            $("input.from").datepicker({
                changeMonth: true,
                numberOfMonths: 3,
                dateFormat: 'yy/mm/dd',
                prevText: '<i class="fa fa-chevron-left"></i>',
                nextText: '<i class="fa fa-chevron-right"></i>',
                onClose: function (selectedDate) {
                    $('input.to').datepicker({
                        minDate: selectedDate
                    });
                }
            });

            $("input.to").datepicker({
                defaultDate: "+1w",
                changeMonth: true,
                numberOfMonths: 3,
                dateFormat: 'yy/mm/dd',
                prevText: '<i class="fa fa-chevron-left"></i>',
                nextText: '<i class="fa fa-chevron-right"></i>',
                onClose: function (selectedDate) {
                    $('input.from').datepicker({
                        maxDate: selectedDate
                    });
                }
            });
        }
    }
} 

var SurveyData: AirSide.Reporting.SurveyorData = new AirSide.Reporting.SurveyorData();
var $imageLoader = $('#imageLoader');

$(document).on('ready', c=> {
    $('label.allAnomalies').css('color', '#fff');
    initMap();
});

$(document).on('click', '#picturesBtn', c=> {
    var fromDate: string = $('input.from').val();
    var toDate: string = $('input.to').val();
    var dateRange: string = fromDate + "-" + toDate;
    var all: boolean = $('#picturesAll').prop('checked');

    SurveyData.dateRange = dateRange;
    SurveyData.all = all;
    SurveyData.surveyType = 1;
    SurveyData.getData();
});

$(document).on('click', '#voiceBtn', c=> {
    var fromDate: string = $('input.from').val();
    var toDate: string = $('input.to').val();
    var dateRange: string = fromDate + "-" + toDate;
    var all: boolean = $('#voiceAll').prop('checked');

    SurveyData.dateRange = dateRange;
    SurveyData.all = all;
    SurveyData.surveyType = 2;
    SurveyData.getData();
});


function showPicture(url: string, long: number, lat: number) {
    //Set the source
    $('#surveyPicture').hide();
    $imageLoader.show();
    $('#surveyPicture').attr('src', url);

    //Check if image has loaded
    var tmpImg = new Image();
    tmpImg.src = $('#surveyPicture').attr('src');
    tmpImg.onload = c=> {
        $imageLoader.hide();
        $('#surveyPicture').fadeIn(300);
    };

    showAssetLocation(lat, long);
}

function showVoice(url: string, long: number, lat: number) {
    $('#audio').hide();
    $('#voiceLoader').fadeIn(300);
    var voice = <HTMLAudioElement>document.getElementById('audio');
    voice.src = url;
    voice.pause();
    voice.play();
    $('#voiceLoader').hide();
    $('#audio').fadeIn(300);
    showAssetLocation(lat, long);
}

function closeAnomaly(guid: string) {
    SurveyData.closeAnomaly(guid);
}

var markers = [];
var size = 500;
var map;

var breakpointDefinition = {
    tablet: 1024,
    phone: 480
};

function clearAllMarkers() {
    if (markers != null) {
        for (var i = 0; i < markers.length; i++) {
            markers[i].setMap(null);
        }
    }
    markers = null;
}

function initMap() {
    $.ajax({
        url: '../../Map/getMapCenter',
        type: 'post',
        dataType: 'json',
        success: (json: Array<number>) => {
            var centerLat = json[0];
            var centerLong = json[1];

            var zoom = 15;
            if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
                zoom = 14;
            }
            var mapOptions = {
                center: new google.maps.LatLng(json[0], json[1]),
                zoom: zoom,
                disableDefaultUI: true,
                mapTypeId: google.maps.MapTypeId.SATELLITE
            };

            map = new google.maps.Map(document.getElementById("map-canvas"),
                mapOptions);
        }
    });
}

function showAssetLocation(lat: number, long: number) {
    map.panTo(new google.maps.LatLng(lat, long));

    clearAllMarkers();
    markers = [];
    var latLongMarker = new google.maps.LatLng(lat, long);
    var image = getImage();
    var marker = new google.maps.Marker({
        map: map,
        position: latLongMarker,
        title: "Surveyor Anomaly",
        icon: image
    });

    markers.push(marker);
}

function getImage() {
    var image = '/images/icons/map_icon_blue.png';
    return image;
}