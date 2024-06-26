/// <reference path="ts/jquery.d.ts" />
var ContainerSynchronizer = /** @class */ (function () {
    function ContainerSynchronizer(container, name, sync_refresh_period, id) {
        if (container === void 0) { container = null; }
        if (name === void 0) { name = null; }
        if (sync_refresh_period === void 0) { sync_refresh_period = 10 * 1000; }
        if (id === void 0) { id = null; }
        var _this = this;
        this.cycle = 0;
        this.is_need_update = false;
        this.is_updating = false;
        this.commands = [];
        if (container == null) {
            document.controller = this;
            ContainerSynchronizer.main = this;
        }
        else
            $(container)[0].controller = this;
        this.container = container != null ? $(container) : $('body');
        //this.server_event = server_event == null ? this.server_web_event : server_event;
        this.container_name = name;
        this.sync_refresh_period = sync_refresh_period;
        this.id = id != null ? id : Math.random().toString();
        ContainerSynchronizer.all[this.id] = this;
        window.setInterval(function () { return _this.update_all(); }, this.sync_refresh_period);
        window.setInterval(function () {
            if (_this.is_need_update) {
                _this.is_need_update = false;
                _this.update_all();
            }
        }, 50);
        this.update_all();
    }
    ContainerSynchronizer.prototype.server_element_event = function (_element, event, data) {
        var element = $(_element);
        var e = null;
        if (event != null) {
            e = {};
            var eventProps = ContainerSynchronizer.eventProps;
            for (var i = 0; i < eventProps.length; ++i)
                e[eventProps[i]] = event[eventProps[i]];
        }
        var element_data = element.data();
        var result_data = {};
        if (element_data.container != null) {
            var container = null;
            var parents = element.parents();
            for (var i = 0; i < parents.length; ++i) {
                if ($(parents[i]).data().name == element_data.container) {
                    container = $(parents[i]);
                    break;
                }
            }
            if (container != null) {
                result_data = $.extend(result_data, container.data());
                var childs = $.merge(container.find('input'), container.find('select'));
                childs = $.merge(childs, container.find('textarea'));
                for (var i = 0; i < childs.length; ++i) {
                    var child = $(childs[i]);
                    var name_1 = child.data().name;
                    if (name_1 == null)
                        continue;
                    if (child.is(':radio') && !child.is(':checked'))
                        continue;
                    if (this.is_array_name(name_1)) {
                        name_1 = name_1.substr(0, name_1.length - 2);
                        if (result_data[name_1] == null)
                            result_data[name_1] = [];
                        var val = this.array_element_value(child);
                        if (val != null)
                            result_data[name_1].push(val);
                    }
                    else
                        result_data[name_1] = this.element_value(child);
                }
            }
        }
        this.server_event({ value: this.element_value(element), checked: element.is(':checked'), data: $.extend(result_data, element_data, data), event: e });
    };
    ContainerSynchronizer.prototype.element_value = function (element) {
        if (element.is(':checkbox'))
            return element.is(':checked');
        if (element.is(':radio'))
            return element.is(':checked') ? element.val() : null;
        return element.val();
    };
    ContainerSynchronizer.prototype.array_element_value = function (element) {
        if (element.is(':radio') || element.is(':checkbox'))
            return element.is(':checked') ? element.val() : null;
        return element.val();
    };
    ContainerSynchronizer.prototype.is_array_name = function (name) {
        return name != null && name.length >= 2 && name.substr(name.length - 2) === '[]';
    };
    ContainerSynchronizer.prototype.find_element = function (current, path) {
        var len = path.length;
        for (var i = 0; i < len; ++i) {
            if (!current)
                return null;
            var pentry = path[i];
            if (pentry.kind == 'element') {
                var childs = current.children;
                current = pentry.index < childs.length ? childs[pentry.index] : null;
            }
        }
        return current;
    };
    ContainerSynchronizer.prototype.is_event_name = function (name) {
        if (!(name.substring(0, 2) === 'on'))
            return false;
        switch (name) {
            case 'onclick':
            case 'ondblclick':
            case 'onmousedown':
            case 'onmousemove':
            case 'onmouseover':
            case 'onmouseout':
            case 'onmouseup':
            case 'onkeydown':
            case 'onkeypress':
            case 'onkeyup':
            case 'onblur':
            case 'onchange':
            case 'onfocus':
            case 'onreset':
            case 'onselect':
            case 'onsubmit':
            case 'onabort':
            case 'onerror':
            case 'onload':
            case 'onresize':
            case 'onscroll':
            case 'onunload':
                return true;
        }
        return false;
    };
    ContainerSynchronizer.prototype.event_on = function (element, event, value) {
        var _this = this;
        if (value != null) {
            element.on(event, function (e) {
                if (value.substr(0, 2) == ';;') {
                    var res = function (sync, e) { return eval(value); }.apply(element.get(0), [_this, e]);
                    if (typeof (res) == 'boolean')
                        return res;
                }
                else {
                    var res = function () { return eval(value); }.apply(element.get(0));
                    if (typeof (res) == 'boolean')
                        return res;
                    _this.server_element_event(element, e);
                }
            });
        }
    };
    ContainerSynchronizer.prototype.set_element = function (element, desc) {
        if (!desc || !element)
            return;
        var len = !desc.e ? 0 : desc.e.length;
        for (var i = 0; i < len; ++i) {
            //window.external.Debug(element[0].tagName != null ? element[0].tagName:element);
            element.append(this.create_element(desc.e[i]));
        }
        //window.external.Debug('attrs');
        var len = !desc.a ? 0 : desc.a.length;
        for (var i = 0; i < len; ++i) {
            if (this.is_event_name(desc.a[i].name)) {
                var event = desc.a[i].name.substring(2);
                var value = desc.a[i].value;
                element.off(event);
                this.event_on(element, event, value);
            }
            else if (desc.a[i].name.substring(0, 5) === 'data-') {
                //element.data(desc.a[i].name.substring(5), desc.a[i].value);
                element[0].setAttribute(desc.a[i].name, desc.a[i].value);
            }
            else {
                element.attr(desc.a[i].name, desc.a[i].value);
            }
        }
        //window.external.Debug('text');
        if (desc.t != null) {
            element.text(desc.t.value);
        }
        if (desc.h != null) {
            element.html(desc.h);
        }
    };
    ContainerSynchronizer.prototype.create_element = function (desc) {
        var element = $(desc.ns ? document.createElementNS(desc.ns, desc.name) : document.createElement(desc.name));
        //window.external.Debug('create_element: ' + desc.a.length);
        var jsInit = null;
        for (var i = 0; i < (!desc.a ? 0 : desc.a.length); ++i) {
            //window.external.Debug('n: ' + desc.a[i].name);
            if (desc.a[i].name == 'js-init')
                jsInit = desc.a[i].value;
        }
        if (jsInit != null) {
            //window.external.Debug('js-init: ' + jsInit);
            !(function () { return eval(jsInit); }.apply(element.get(0)));
            //var _this = element;
            //eval(jsInit);
        }
        this.set_element(element, desc);
        return element;
    };
    ContainerSynchronizer.prototype.change_element = function (current, cmd, desc) {
        if (!current)
            return;
        switch (cmd) {
            case 'remove':
                current.remove();
                break;
            case 'clear':
                current.empty();
                break;
            case 'clear-all':
                current.empty();
                var attributes = $.map(current[0].attributes, function (item) { return item.name; });
                $.each(attributes, function (i, item) { return current.removeAttr(item); });
                break;
            case 'set':
                this.set_element(current, desc);
                break;
            case 'after':
                current.after(this.create_element(desc));
                break;
            case 'insert':
                current.prepend(this.create_element(desc));
                break;
            case 'js-update':
                !(function () { return eval(desc); }.apply(current.get(0)));
                //var _this = current;
                //eval(<string><any>desc);
                break;
        }
    };
    ContainerSynchronizer.prototype.apply_commands = function (commands) {
        var len = commands.length;
        for (var i = 0; i < len; ++i) {
            var command = commands[i];
            this.change_element($(this.find_element(this.container.get(0), command.path)), command.cmd, command.value);
        }
    };
    ContainerSynchronizer.prototype.sync = function (data) {
        if (data.prev_cycle == this.cycle && !this.is_updating) {
            this.is_updating = true;
            try {
                this.apply_commands(data.updates);
                this.cycle = data.cycle;
                this.commands = this.commands.slice(data.processed_commands != null ? data.processed_commands : 0);
                if (this.commands.length > 0)
                    this.is_need_update = true;
            }
            finally {
                this.is_updating = false;
            }
        }
        else {
            this.is_need_update = true;
        }
    };
    ContainerSynchronizer.prototype.server_event = function (json) {
        this.commands.push((typeof json === 'string') ? JSON.parse(json) : json);
        //$.post(this.js_path(), JSON.stringify({ 'frame': this.id, 'cycle': this.cycle, 'commands': this.commands }), data => this.sync(data), 'json');
        this.post({ 'frame': this.id, 'cycle': this.cycle, 'commands': this.commands });
    };
    ContainerSynchronizer.prototype.update_all = function () {
        try {
            if (this.commands.length > 0) {
                //$.post(this.js_path(), JSON.stringify({ 'frame': this.id, 'cycle': this.cycle, 'commands': this.commands }), data => this.sync(data), 'json');
                this.post({ 'frame': this.id, 'cycle': this.cycle, 'commands': this.commands });
            }
            else {
                //$.post(this.js_path(), JSON.stringify({ 'frame': this.id, 'cycle': this.cycle }), data => this.sync(data), 'json');
                this.post({ 'frame': this.id, 'cycle': this.cycle });
            }
        }
        catch (e) {
            console.log(e);
        }
    };
    ContainerSynchronizer.prototype.post = function (data) {
        var _this = this;
        if (typeof fetch == "function") {
            fetch(this.js_path(), {
                method: "POST",
                body: JSON.stringify(data),
                credentials: 'include',
                headers: new Headers({ 'Content-Type': 'application/json' })
            })
                .then(function (response) { return response.json(); })
                .then(function (json) { return _this.sync(json); });
        }
        else
            this.jquery_post(data);
    };
    ContainerSynchronizer.prototype.jquery_post = function (data) {
        var _this = this;
        //$.post(this.js_path(), JSON.stringify(data), data => this.sync(data), 'json');
        $.ajax({
            type: 'POST',
            data: JSON.stringify(data),
            dataType: 'json',
            success: function (data) { return _this.sync(data); },
            jsonp: false
        });
    };
    ContainerSynchronizer.prototype.js_path = function (query) {
        var path = this.container_name;
        if (path == null)
            path = window.location.href;
        if (query != null && query != '') {
            if (path.indexOf('?') < 0)
                path += '?' + query;
            else
                path += '&' + query;
        }
        return path;
    };
    ContainerSynchronizer.all = {};
    ContainerSynchronizer.main = null;
    ContainerSynchronizer.eventProps = ['type', 'bubbles', 'cancelable', 'eventPhase', 'timeStamp',
        'button', 'clientX', 'clientY', 'screenX', 'screenY',
        'keyIdentifier', 'keyLocation', 'keyCode', 'charCode', 'which',
        'altKey', 'ctrlKey', 'metaKey', 'shiftKey'
    ];
    return ContainerSynchronizer;
}());
var Command = /** @class */ (function () {
    function Command() {
    }
    return Command;
}());
var ElementDescription = /** @class */ (function () {
    function ElementDescription() {
    }
    return ElementDescription;
}());
var AttributeDescription = /** @class */ (function () {
    function AttributeDescription() {
    }
    return AttributeDescription;
}());
var TextDescription = /** @class */ (function () {
    function TextDescription() {
    }
    return TextDescription;
}());
var PathEntry = /** @class */ (function () {
    function PathEntry() {
    }
    return PathEntry;
}());
//# sourceMappingURL=NitroBolt.Wui.2.0.48.js.map