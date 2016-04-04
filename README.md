# po-files-reader-mvc
po files reader and helpers for asp.net mvc &lt;6

the base code comes from orchard cms.
Simplified, and adapted to be used in a asp.net project 
Works ok but would need some improvements like :

TODO : 

- Persistence container within html mvc helpers
- Ilogger interface to plug
- unit tests
- 


For helpers : files should be in App_Data/Localization/{cultureUI}/
can be changed. 

fallback mechanism on culture parent : en-US > en > neutral
