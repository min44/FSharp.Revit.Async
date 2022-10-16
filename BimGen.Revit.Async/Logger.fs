module private BimGen.Revit.Async.Logger

type CustomLogger() =
    static member Debug _ = ()
    
type internal Logger() =
    static member LogOn = false
    static member Debug(text) =
        if Logger.LogOn 
        then CustomLogger.Debug($"{text}")
        else ()