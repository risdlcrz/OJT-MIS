// Simple workflow helper (localStorage demo)
(function (global) {
    const key = 'ojt_workflow_v1';
    const defaultState = {
        login: false,
        intern_request: false,
        applicants: false,
        hiring_info: false,
        check_requirements: false,
        intern_approval: false,
        orientation: false,
        generate_id: false,
        attendance: false,
        monitoring: false,
        certificate: false
    };

    function load() {
        try {
            const raw = localStorage.getItem(key);
            return raw ? JSON.parse(raw) : Object.assign({}, defaultState);
        } catch (e) {
            return Object.assign({}, defaultState);
        }
    }
    function save(state) {
        localStorage.setItem(key, JSON.stringify(state));
    }
    function set(step, value) {
        const s = load();
        s[step] = !!value;
        save(s);
        return s;
    }
    function is(step) {
        return !!load()[step];
    }
    function requireSteps(steps) {
        const s = load();
        const missing = steps.filter(st => !s[st]);
        return { ok: missing.length === 0, missing };
    }
    function reset() { localStorage.removeItem(key); }
    function state() { return load(); }

    global.workflow = { set, is, requireSteps, reset, state };
})(window);