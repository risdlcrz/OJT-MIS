// Integrates workflow with sidebar menu (run after menu.js)
document.addEventListener('DOMContentLoaded', function () {
    if (typeof workflow === 'undefined') return;

    // Map page -> required steps (adjust as needed)
    const prereqs = {
        'Request.aspx': ['login'],
        'Applicants.aspx': ['intern_request'],
        'OJT.aspx': ['applicants'],
        'ScheduleOrientation.aspx': ['intern_approval'],
        'IDGenerate.aspx': ['orientation', 'intern_approval'],
        'DTR.aspx': ['generate_id'],
        'InternList.aspx': ['attendance'],
        'Certificate.aspx': ['monitoring'] // example
    };

    Object.keys(prereqs).forEach(page => {
        const result = workflow.requireSteps(prereqs[page]);
        const a = document.querySelector(`#sidebarMenu a[href="${page}"]`);
        if (a && !result.ok) {
            a.classList.add('disabled');
            a.removeAttribute('href');
            a.setAttribute('title', 'Complete prior steps first: ' + result.missing.join(', '));
            a.style.opacity = '0.6';
            a.style.pointerEvents = 'none';
        } else if (a && result.ok) {
            // optionally mark as available
            a.style.opacity = '';
            a.style.pointerEvents = '';
            a.removeAttribute('title');
        }
    });
});