(function ($) {

    // get full window size
    $(window).on('load resize', function () {
        var w = $(window).width();
        var h = $(window).height();

        $('section').height(h);
    });

    // scrollTo plugin
    $('#signup_from_1').scrollTo({ easing: 'easeInOutQuint', speed: 1500 });
    $('#forgot_from_1').scrollTo({ easing: 'easeInOutQuint', speed: 1500 });
    $('#signup_from_2').scrollTo({ easing: 'easeInOutQuint', speed: 1500 });
    $('#forgot_from_2').scrollTo({ easing: 'easeInOutQuint', speed: 1500 });
    $('#forgot_from_3').scrollTo({ easing: 'easeInOutQuint', speed: 1500 });


    // set focus on input
    var firstInput = $('section').find('input[type=text], input[type=email]').filter(':visible:first');

    if (firstInput != null) {
        firstInput.focus();
    }

    $('section').waypoint(function (direction) {
        var target = $(this).find('input[type=text], input[type=email]').filter(':visible:first');
        target.focus();
    }, {
        offset: 300
    }).waypoint(function (direction) {
        var target = $(this).find('input[type=text], input[type=email]').filter(':visible:first');
        target.focus();
    }, {
        offset: -400
    });


    // animation handler
    $('[data-animation-delay]').each(function () {
        var animationDelay = $(this).data("animation-delay");
        $(this).css({
            "-webkit-animation-delay": animationDelay,
            "-moz-animation-delay": animationDelay,
            "-o-animation-delay": animationDelay,
            "-ms-animation-delay": animationDelay,
            "animation-delay": animationDelay
        });
    });

    $('[data-animation]').waypoint(function (direction) {
        if (direction == "down") {
            $(this).addClass("animated " + $(this).data("animation"));
        }
    }, {
        offset: '90%'
    }).waypoint(function (direction) {
        if (direction == "up") {
            $(this).removeClass("animated " + $(this).data("animation"));
        }
    }, {
        offset: '100%'
    });

})(jQuery);