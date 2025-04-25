using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MolcaEtiquetadoManual.Data.StoredProcedures
{
    // Data/StoredProcedures/StoredProcedureDefinitions.cs
    public static class StoredProcedureDefinitions
    {
        public static string SP_ObtenerSiguienteNumeroPallet => @"
        CREATE PROCEDURE SP_ObtenerSiguienteNumeroPallet
            @ProgramaProduccion VARCHAR(50)
        AS
        BEGIN
            DECLARE @MaxSec INT
            
            SELECT @MaxSec = ISNULL(MAX(SEC), 0)
            FROM SALIDA
            WHERE DOCO = @ProgramaProduccion
            
            SELECT @MaxSec + 1 AS SiguienteNumeroPallet
        END
    ";

        public static string SP_InsertarEtiqueta => @"
         CREATE PROCEDURE [dbo].[SP_InsertarEtiqueta]
                        @EDUS VARCHAR(10),
						@EDDT VARCHAR(10),
                        @EDTN VARCHAR(10),
                        @EDLN INT,
                        @DOCO VARCHAR(max),
                        @LITM VARCHAR(20),
                        @SOQS INT,
                        @UOM VARCHAR(5),
                        @LOTN VARCHAR(20),
                        @EXPR DATETIME,
                        @TDAY VARCHAR(10),
                        @SHFT VARCHAR(5),
                        @URDT DATETIME,
                        @ESTADO VARCHAR(5),
                        @URRF VARCHAR(10),
						@Confirmada bit
                    AS
                    BEGIN
                        DECLARE @NumeroPallet INT
                
                        -- Obtener el siguiente número de pallet
                        SELECT @NumeroPallet = ISNULL(MAX(SEC), 0) + 1
                        FROM SALIDA
                        WHERE DOCO = @DOCO
                
                        -- Insertar el registro
                        INSERT INTO SALIDA (
                            EDUS, EDDT, EDTN, EDLN, DOCO, LITM, SOQS, 
                            UOM, LOTN, EXPR, TDAY, SHFT, URDT, SEC, ESTADO, URRF,FechaCreacion,Confirmada
                        )
                        VALUES (
                            @EDUS, @EDDT , @EDTN, @EDLN, @DOCO, @LITM, @SOQS,
                            @UOM, @LOTN, @EXPR, @TDAY, @SHFT, @URDT, @NumeroPallet, @ESTADO, @URRF,GETDATE(),@Confirmada
                        )
                
                        -- Retornar el número de pallet asignado
                        SELECT @NumeroPallet AS NumeroPallet
                    END
    
    ";
    }
}
