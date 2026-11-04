// Info-Panels beim Scrollen einblenden. Ohne IntersectionObserver bleiben sie sichtbar.
(function () {
  var panels = document.querySelectorAll('.panel');
  if (!('IntersectionObserver' in window)) {
    panels.forEach(function (panel) { panel.classList.add('visible'); });
    return;
  }
  var observer = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.18 });
  panels.forEach(function (panel) { observer.observe(panel); });
})();
