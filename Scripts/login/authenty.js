(function($) {
	$('input').iCheck({
    checkboxClass: 'icheckbox_minimal-orange',
    radioClass: 'iradio_minimal-orange',
    increaseArea: '20%' 
	});
	var rememberMeChecked = false;
	$('.iCheck-helper').click(function () {
	    if (rememberMeChecked) rememberMeChecked = false; else rememberMeChecked = true;
	    console.log(rememberMeChecked);
	    $('#RememberMe').val(rememberMeChecked);
	});
	
	$('#signIn_1').click(function (e) {  
	   
	    var username = $.trim($('#Email').val());
	    var password = $.trim($('#Password').val());

	    if ( username === '' || password === '' ) {
	        $('#form_1 .adb_logo').removeClass('success').addClass('fail');
	        $('#form_1').addClass('fail');
	        return false;
	    } else {
	        $('#form_1 .adb_logo').removeClass('fail').addClass('success');
			$('#form_1').removeClass('fail').removeClass('animated');
	    }
	});

})(jQuery);