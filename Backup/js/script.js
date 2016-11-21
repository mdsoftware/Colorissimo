function submitEvent(event) {
    var e = document.getElementById('form_event');
    if (e) {
        e.value = event;
        var f = document.getElementById('form1');
        if (f) {
            f.submit();
        } else {
            alert('Form not found');
        }
    }
}

function disableButton(buttonId) {
    var btn = document.getElementById(buttonId);
    if (btn) {
        var tds = btn.getElementsByTagName('td');
        if (tds.length == 2) {
            tds[0].className = 'button_disabled';
            tds[0].onclick = null;
            tds[1].className = 'button_disabled';
            tds[1].onclick = null;
        }
    }
}

function colorChangedSync(x, sampleId, infoId) {
    var s = x.value;

    var oldV = x.oldValue;
    if ((oldV != null) && (s != null) && (oldV == s))
        return;

    x.oldValue = s;

    var info = document.getElementById(infoId);
    if (info) {
        var uri = "handler.ashx?q=colorinfo&t=" + encodeURIComponent(s);
        var response = syncGetRequest(uri);
        if (response.status == 200) {
            info.innerHTML = response.responseText;
        } else {
            info.innerHTML = 'Error occured';
        }
    }

    var sample = document.getElementById(sampleId);
    if (sample) {
        var uri = "handler.ashx?q=colorsample&t=" + encodeURIComponent(s);
        var response = syncGetRequest(uri);
        if (response.status == 200) {
            sample.innerHTML = response.responseText;
        } else {
            sample.innerHTML = 'Error occured';
        }
    }

}

function colorChanged(x, sampleId, infoId) {
    var s = x.value;

    var oldV = x.oldValue;
    if ((oldV != null) && (s != null) && (oldV == s))
        return;

    x.oldValue = s;

    var info = document.getElementById(infoId);
    if (info) {

        // info.innerHTML = '<img src="img/wait.gif"/>';

        var uri = "handler.ashx?q=colorinfo&t=" + encodeURIComponent(s);
        asyncGetRequest(uri, infoId);
    }

    var sample = document.getElementById(sampleId);
    if (sample) {

        sample.innerHTML = '<img src="img/wait.gif"/>';

        var uri = "handler.ashx?q=colorsample&t=" + encodeURIComponent(s);
        asyncGetRequest(uri, sampleId);
    }
}

function syncGetRequest(url) {
    var request = false;
    try {
        request = new XMLHttpRequest();
    } catch (trymicrosoft) {
        try {
            request = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (othermicrosoft) {
            try {
                request = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (failed) {
                request = false;
            }
        }
    }

    if (!request)
        alert("Error initializing server request");

    request.open("GET", url, false);
    request.send(null);

    return request;
}

function asyncGetRequest(url, objectId) {
    var request = false;
    try {
        request = new XMLHttpRequest();
    } catch (trymicrosoft) {
        try {
            request = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (othermicrosoft) {
            try {
                request = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (failed) {
                request = false;
            }
        }
    }

    if (!request)
        alert("Error initializing server request");

    request.open("GET", url, true);

    request.onreadystatechange = function () {
        if (request.readyState == 4) {
            var o = document.getElementById(objectId);
            if (o) {
                if (request.status == 200) {
                    o.innerHTML = request.responseText;
                } else {
                    o.innerHTML = 'Error occured';
                }
            }
        }
    };

    request.send(null);

    return request;
}


