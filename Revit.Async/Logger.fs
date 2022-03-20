namespace Revit.Async

type CustomLogger() =
    static member Debug text = ()
    
type internal Logger() =
    static member LogOn = false
    static member Debug(text) =
        if Logger.LogOn 
        then CustomLogger.Debug($"{text}")
        else ()