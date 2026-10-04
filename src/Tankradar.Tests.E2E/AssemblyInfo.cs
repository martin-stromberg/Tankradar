// Jeder E2E-Test startet eine eigene App-Instanz und bedient die Desktop-Oberfläche; paralleler Lauf ist instabil.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
