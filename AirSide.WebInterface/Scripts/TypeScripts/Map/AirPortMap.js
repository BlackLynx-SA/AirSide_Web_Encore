/// <reference pth="../../scripts/plugin/googlemaps/MarkerCluster.js" />

//Map Global Variables
var Assets = new AllAssets();
var SubAreas = new AllSubAreas();
var map, heatmap, infowindow;
var markers = [];
var markerClusterer = new MarkerClusterer(null, null);
var overlayShow = false;
var heatMapShow = false;
var clusterShow = false;
var centerLat, centerLong;
var rectangle;
var rectFlag = false;
var selectedTask = 0;
var lastFilter, filterEnum, filterValue;

initMap();

function setTask(id, desc) {
    selectedTask = id;
    $('#taskLabel').html(desc);
    if (lastFilter === "assets")
        window.filterAssets();
    else if (lastFilter === "areas")
        window.filterAreas();
}

function AllAssets() {
    this.jsonData = {};
}

function showAllAssets() {
    markers = [];
    $.each(Assets.jsonData, function(i, v) {
        if (selectedTask !== 0) {
            $.each(v.maintenance, function(index, value) {
                if (value.maintenanceId === selectedTask) {
                    addMarker(v);
                    return false;
                }
            });
        } else {
            addMarker(v);
        }
    });
}

function markerInfo(json) {

    var previousDate, nextDate, cycle;

    if (selectedTask !== 0) {
        $.each(json.maintenance, function(i, v) {
            if (v.maintenanceId === selectedTask) {
                previousDate = v.previousDate;
                nextDate = v.nextDate;
                cycle = v.maintenanceCycle;
            }
        });
    } else {
        var v = json.maintenance[0];
        previousDate = v.previousDate;
        nextDate = v.nextDate;
        cycle = v.maintenanceCycle;
    }

    //Header
    var content = '<div class="mapInfo" style="width:300px;"><h3 class="header smaller lighter blue">' + json.serialNumber + '<small> - (' + json.rfidTag + ')</small></h3>';

    //Image of Asset
    content += '<div style="width:100%; text-align:center;"><img src="' + json.picture.fileLocation + '"/></div><hr/>';
    content += "<h5 class='txt-color-blue'>Summary</h4>"
    content += "Maintenance Status: " + getMaintenanceStatus(cycle) + "<br />";
    content += "Previous Date: <strong>" + previousDate + "</strong><br />";
    content += "Next Date: <strong>" + nextDate + "</strong><br />";
    content += "Asset Type: <strong>" + json.assetClass.description + "</strong><br />";
    if (selectedTask === 0)
        content += "Maintenance Task: <strong>" + json.maintenance[0].maintenanceTask + "</strong><br />";
    content += "<br/><hr/><a class='btn btn-sm btn-success' href='../../History/AssetHistory?id=" + json.assetId.toString() + "'><i class='fa fa-clock-o'></i> History</a>";
    content += "<a class='btn btn-sm btn-primary margin-left-5' href='" + json.productUrl + "' target='_blank'><i class='fa fa-book'></i> Manual</a><br />";

    content += "</ul>";
    content += '<hr/>';
    if (json.status === true) {
        content += '<form class="smart-form"><label class="toggle"><input class="faultyLightToggle" type="checkbox" name="faultyLightToggle" data-asset-id="' + json.assetId + '_faultyToggle" checked="checked"><i data-swchon-text="ON" data-swchoff-text="OFF"></i>Light Faulty</label></form>';
    } else {
        content += '<form class="smart-form"><label class="toggle"><input class="faultyLightToggle" type="checkbox" name="faultyLightToggle" data-asset-id="' + json.assetId + '_faultyToggle"><i data-swchon-text="ON" data-swchoff-text="OFF"></i>Light Faulty</label></form>';
    }
    //End
    content += '</div>';

    return content;
}

function getMaintenanceStatus(status) {
    var img;
    switch (status) {
        case 0:
            img = "<span class='txt-color-blue'><i class='fa fa-exclamation-triangle'></i> No Data Available</span>";
            break;
        case 1:
            img = "<span class='txt-color-green'><i class='fa fa-thumbs-o-up'></i> Recently Updated</span>";
            break;
        case 2:
            img = "<span class='txt-color-yellow'><i class='fa fa-ellipsis-h'></i> In Mid Cycle</span>";
            break;
        case 3:
            img = "<span class='txt-color-orange'><i class='fa fa-bell icon-animated-bell'></i> Almost Due</span>";
            break;
        case 4:
            img = "<span class='txt-color-red'><i class='fa fa-thumbs-o-down'></i> Asset Overdue</span>";
            break;
        default:
            break;
    }

    return img
}

function getImage(value, status) {
    var image, cycle;
    if (status === false) {
        if (selectedTask === 0) {
            cycle = value[0].maintenanceCycle;
        } else {
            $.each(value, function(i, v) {
                if (v.maintenanceId === selectedTask)
                    cycle = v.maintenanceCycle;
            });
        }

        switch (cycle) {
            case 0:
                image = '/images/map_images/NutralMarker.png';
                break;
            case 1:
                image = '/images/map_images/GreenMarker.png';
                break;
            case 2:
                image = '/images/map_images/YellowMarker.png';
                break;
            case 3:
                image = '/images/map_images/OrangeMarker.png';
                break;
            case 4:
                image = '/images/map_images/RedMarker.png';
                break;

            default:
                break;
        }

        return image;
    } else {
        return '/images/map_images/exclamation.png';
    }
}

//This needs to be updated to accomodate for multiple AutoCAD Drawings
function loadOverlay() {
    $('#loader').fadeIn(500);
    if (!overlayShow) {
        var overlaySrc = '../../images/map_images/wholemap.gif';
        var preloadOverlay = new Image();
        preloadOverlay.onload = function() {

            var imagebounds = new google.maps.LatLngBounds(
                new google.maps.LatLng(51.455567, -0.490753),
                new google.maps.LatLng(51.479600, -0.426184));
            mapOver = new google.maps.GroundOverlay(overlaySrc, imagebounds);
            mapOver.setMap(map);
            overlayShow = true;
            $('#loader').fadeOut(500);
        };
        preloadOverlay.src = overlaySrc;
    } else {
        mapOver.setMap(null);
        overlayShow = false;
        $('#loader').fadeOut(500);
    }
}

function addMarker(json) {
    var long = json.location.longitude;
    var lat = json.location.latitude;
    var latLongMarker = new google.maps.LatLng(lat, long);
    var image = getImage(json.maintenance, json.status);
    var marker = new google.maps.Marker({
        map: map,
        position: latLongMarker,
        title: json.serialNumber,
        icon: image
    });

    //Generate Info Window Content
    var content = markerInfo(json);

    google.maps.event.addListener(marker, 'click', function() {
        if (infowindow) infowindow.close();
        infowindow = new google.maps.InfoWindow({ content: content });
        infowindow.open(map, marker);
    });

    markers.push(marker);
}

function applySearchFilter() {
    var searchStr = $('#search-fld').val();

    //Clear all lights from map
    clearAllMarkers();
    markerClusterer.clearMarkers();

    markers = [];

    Assets.jsonData.forEach(function (c) {
        var item = c.serialNumber.toLowerCase();
        var item2 = c.rfidTag.toLowerCase();

        if (item.indexOf(searchStr.toLowerCase()) >= 0 || item2.indexOf(searchStr.toLowerCase()) >= 0) {
            addMarker(c);
        }
    });
}

function changeMap(type) {
    if (type === 'Satellite') {
        map.setMapTypeId(google.maps.MapTypeId.SATELLITE);
    } else if (type === 'Roadmap') {
        map.setMapTypeId(google.maps.MapTypeId.ROADMAP);
    } else if (type === 'Hybrid') {
        map.setMapTypeId(google.maps.MapTypeId.HYBRID);
    } else if (type === 'Terrain') {
        map.setMapTypeId(google.maps.MapTypeId.TERRAIN);
    }
}

function AllAssets() {
    this.jsonData = {};
}

function AllSubAreas() {
    this.jsonData = {};
}

function requestAllAssetData() {
    $.ajax({
        url: 'getAllAssets',
        type: 'post',
        dataType: 'json',
        success: function(json) {
            Assets.jsonData = json;
            $('#mapLoader').hide();
            $('#mapMenuItems').fadeIn(300);
            filterAssets(true);
        }
    });
}

function requestAllSubAreas() {
    $.ajax({
        url: 'getAllSubAreas',
        type: 'post',
        dataType: 'json',
        success: function(json) {
            SubAreas.jsonData = json;
        }
    });
}

function initMap() {
    $.ajax({
        url: 'getMapCenter',
        type: 'post',
        dataType: 'json',
        success: function(json) {
            centerLat = json[0];
            centerLong = json[1];

            var zoom = 15;
            if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
                zoom = 14;
            }
            var mapOptions = {
                center: new google.maps.LatLng(json[0], json[1]),
                zoom: zoom,
                mapTypeId: google.maps.MapTypeId.SATELLITE,
            };
            map = new google.maps.Map(document.getElementById("map-canvas"),
                mapOptions);

            //Download all assets and Sub Areas
            requestAllAssetData();
            requestAllSubAreas();


        }
    });
}

function toggleFaultyLight(id, flag) {
            
    Assets.jsonData.forEach(function(e) {
        if (e.assetId.toString() === id) {
            e.status = flag;
            var i = 0;
            markers.forEach(function(d) {
                if(d.title === id)
                    markers[i].setMap(null);
                i++;
            });
                    
            addMarker(e);
        }
                
    });
}

function resize() {
    //Calculate the window height and deduct the menu height
    $('#map-canvas').css('height', Math.round($(window).height() - 170));
    $('#mapTopMenuBack').css('width', $('#map-canvas').width());
    $('#mapMenuItems').css('width', $('#map-canvas').width());
    if (map !== null)
        google.maps.event.trigger(map, 'resize');
}

function clearAllMarkers() {
    if (markers !== null) {
        for (var i = 0; i < markers.length; i++) {
            markers[i].setMap(null);
        }
        markers = null;
    }
}


$(document).ready(function() {
    $('#map-canvas').css('height', Math.round($(window).height() - 170));
    $('#mapTopMenuBack').css('width', $('#map-canvas').width());
    $('#mapMenuItems').css('width', $('#map-canvas').width());
    $('#timepicker').timepicker();

    //Init Date Range
    $("input.from").datepicker({
        changeMonth: true,
        numberOfMonths: 3,
        dateFormat: 'yy/mm/dd',
        prevText: '<i class="fa fa-chevron-left"></i>',
        nextText: '<i class="fa fa-chevron-right"></i>',
        onClose: function(selectedDate) {
            $('input.to').datepicker({
                minDate: selectedDate
            });
        },
        beforeShow: function() {
            setTimeout(function() {
                $('.ui-datepicker').css('z-index', 9999);
            }, 0);
        }
    });

    $("input.to").datepicker({
        defaultDate: "+1w",
        changeMonth: true,
        numberOfMonths: 3,
        dateFormat: 'yy/mm/dd',
        prevText: '<i class="fa fa-chevron-left"></i>',
        nextText: '<i class="fa fa-chevron-right"></i>',
        onClose: function(selectedDate) {
            $('input.from').datepicker({
                maxDate: selectedDate
            });
        },
        beforeShow: function() {
            setTimeout(function() {
                $('.ui-datepicker').css('z-index', 9999);
            }, 0);
        }
    });

    $(document).on('change', '#mainAreas', function() {
        var area = $(this).val();
        window.filterSubAreas(area);
    });

    $(document).on('click', '#assetFilterBtn', function() {
        window.filterAssets();
    });

    $(document).on('click', '#areaFilterBtn', function() {
        window.filterAreas();
    });

    $(document).on('click', '#surveyorFilterBtn', function() {
        window.requestSurveyorData();
    });

    $(document).on('click', '#photoFilterBtn', function() {
        window.applyFbTechFilter();
    });

    //Modal Click Events
    $(document).on('click', '.assetFilterBtn', function() {
        $('#assetFilterModal').modal('show');
    });

    $(document).on('click', '.areaFilterBtn', function() {
        $('#areaFilterModal').modal('show');
    });

    $(document).on('click', '.surveyorBtn', function() {
        $('#visualSurveyFilterModal').modal('show');
    });

    $(document).on('click', '.photoBtn', function() {
        $('#photometricFilterModal').modal('show');
    });

    $(document).on('click', '.shiftsBtn', function() {
        window.showRectangle();
    });

    $(document).on('click', '.autoCadBtn', function() {
        loadOverlay();
    });

    $(document).on('click', '.faultyLightToggle', function(e) {
        var item = $(e.target);
        var assetId = '';
        var checked = false;
        if (item.prop('checked')) {
            assetId = item.attr('data-asset-id');
            assetId = assetId.replace('_faultyToggle', '');
            checked = true;
        } else {
            assetId = item.attr('data-asset-id');
            assetId = assetId.replace('_faultyToggle', '');
            checked = false;
        }

        var data = {
            assetId: assetId,
            flag: checked
        };

        $.ajax({
            url: 'UpdateFaultyLight',
            type: 'post',
            data: data,
            dataType: 'json',
            success: function() {
                toggleFaultyLight(assetId, checked);
            }
        });
    });

    $(document).on('click', '#faultyFilterBtn', function() {
        window.filterFaulty();
    });

    //Select the radio button on drop down select
    $(document).on('click', '#assetClasses', function() {
        $('#assetTypeRadio').prop("checked", null);
        $('#allAssetRadio').prop("checked", null);
        $('#assetTypeRadio').prop("checked", "checked");
    });

    $(document).on('click', '#maintenanceTasks', function() {
        $('#assetTypeRadio').prop("checked", null);
        $('#allAssetRadio').prop("checked", null);
        $('#assetMaintenanceRadio').prop("checked", "checked");
    });

    $(document).on('click', '#assetClasses', function() {});

    //Fire all the resize items for the map
    $(window).resize(function() { resize() });

    $(document).on('click', '#searchBtn', function(c) {
        applySearchFilter();
    });
});