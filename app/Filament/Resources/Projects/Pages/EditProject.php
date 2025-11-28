<?php

namespace App\Filament\Resources\Projects\Pages;

use App\Filament\Resources\Projects\ProjectResource;
use Filament\Resources\Pages\EditRecord;
use App\Models\Process;
use App\Models\SDGStatus;
use Filament\Exceptions\FormsValidationException;

class EditProject extends EditRecord
{
    protected static string $resource = ProjectResource::class;
    protected static ?string $title = 'Re-Attempt Project'; // Page heading


    protected array $processData = [];
    protected array $sdgTotals = [];

    /**
     * ---------------------------------------------------------
     *  LOAD EXISTING DATA INTO FORM
     * ---------------------------------------------------------
     */
    protected function mutateFormDataBeforeFill(array $data): array
    {
        $process = Process::where('project_id', $this->record->id)->first();

        if ($process) {
            // All phases: load raw arrays for form
            $data['initiation_items'] = $process->initiation_items ?? [];
            $data['planning_items']   = $process->planning_items ?? [];

            $execution = $process->execution_items ?? [];
            $data['process']['execution_env']   = $execution['execution_env']   ?? [];
            $data['process']['execution_waste'] = $execution['execution_waste'] ?? [];
            $data['process']['execution_energy']= $execution['execution_energy']?? [];
            $data['process']['execution_water'] = $execution['execution_water'] ?? [];
            $data['process']['execution_ieq']   = $execution['execution_ieq']   ?? [];

            $data['process']['monitoring'] = $process->monitoring_items ?? [];
            $data['process']['closing'] = $process->closing_items ?? [];
        }

        return $data;
    }

    /**
     * ---------------------------------------------------------
     *  BEFORE SAVE — PROCESS FORM DATA & SDG SCORING
     * ---------------------------------------------------------
     */
    protected function mutateFormDataBeforeSave(array $data): array
    {
        // Collect all checkbox selections
        $this->processData = [
            'initiation_items' => $data['initiation_items'] ?? [],
            'planning_items' => $data['planning_items'] ?? [],
            'execution_items' => [
                'execution_env'   => $data['process']['execution_env']   ?? [],
                'execution_waste' => $data['process']['execution_waste'] ?? [],
                'execution_energy'=> $data['process']['execution_energy']?? [],
                'execution_water' => $data['process']['execution_water'] ?? [],
                'execution_ieq'   => $data['process']['execution_ieq']   ?? [],
            ],
            'monitoring_items' => $data['process']['monitoring'] ?? [],
            'closing_items'    => $data['process']['closing'] ?? [],
        ];

        // Remove raw form data from project table
        unset($data['initiation_items'], $data['planning_items'], $data['process']);

        // Initialize SDG totals
        $this->sdgTotals = array_fill_keys([
            'sdg3','sdg6','sdg7','sdg8','sdg9','sdg11','sdg12','sdg13','sdg15'
        ], 0);

        // SDG map and marks (same as CreateProject)
        $sdgMap = $this->getSdgMap();
        $marks = $this->getMarks();

        $calculateStage = function(array $items, array $marks) use ($sdgMap) {
            $total = 0;
            $sdgStage = [];
            foreach ($items as $item) {
                $points = $marks[$item] ?? 0;
                $total += $points;

                if (!empty($sdgMap[$item])) {
                    $sdgs = array_map('trim', explode(',', $sdgMap[$item]));
                    $splitPoints = $points / count($sdgs);
                    foreach ($sdgs as $sdg) {
                        $sdgStage[$sdg] = ($sdgStage[$sdg] ?? 0) + $splitPoints;
                    }
                }
            }
            return [$total, $sdgStage];
        };

        // Process each phase
        $this->phaseTotals = [];
        [$this->phaseTotals['initiation'], $sdgStage] = $calculateStage($this->processData['initiation_items'], $marks['initiation']);
        $this->addSdg($sdgStage);
        [$this->phaseTotals['planning'], $sdgStage] = $calculateStage($this->processData['planning_items'], $marks['planning']);
        $this->addSdg($sdgStage);

        $executionItems = array_merge(
            $this->processData['execution_items']['execution_env'],
            $this->processData['execution_items']['execution_waste'],
            $this->processData['execution_items']['execution_energy'],
            $this->processData['execution_items']['execution_water'],
            $this->processData['execution_items']['execution_ieq'],
        );
        [$this->phaseTotals['execution'], $sdgStage] = $calculateStage($executionItems, $marks['execution']);
        $this->addSdg($sdgStage);

        [$this->phaseTotals['monitoring'], $sdgStage] = $calculateStage($this->processData['monitoring_items'], $marks['monitoring']);
        $this->addSdg($sdgStage);

        [$this->phaseTotals['closing'], $sdgStage] = $calculateStage($this->processData['closing_items'], $marks['closing']);
        $this->addSdg($sdgStage);

        return $data;
    }

    protected function afterSave(): void
{
    $projectId = $this->record->id;

    // Calculate process score
    $score = array_sum($this->phaseTotals);
    $processStatus = match (true) {
        $score >= 86 => 'PLATINUM',
        $score >= 76 => 'GOLD',
        $score >= 66 => 'SILVER',
        $score >= 50 => 'CERTIFIED',
        default => 'FAIL',
    };

    // Save Process
    Process::updateOrCreate(
        ['project_id' => $projectId],
        array_merge($this->phaseTotals, $this->processData, ['status' => $processStatus])
    );

    // Save SDG
    $sdgScore = array_sum(array_map(fn($v)=> (int)$v, $this->sdgTotals));
    $sdgStatus = match (true) {
        $sdgScore >= 89 => 'A',
        $sdgScore >= 69 => 'B',
        $sdgScore >= 49 => 'C',
        $sdgScore >= 40 => 'D',
        default => 'FAIL',
    };

    SDGStatus::updateOrCreate(
        ['project_id' => $projectId],
        array_merge(array_map(fn($v) => (int)$v, $this->sdgTotals), ['status'=>$sdgStatus])
    );

    // -------------------------------------------------
    // Reset project status to a valid value for re-attempt
    // -------------------------------------------------
    $this->record->update([
        'status' => 'none', // replace with your ENUM default or re-attempt value
    ]);
}

    // ---------------------------------------------------------
    // HELPER FUNCTIONS
    // ---------------------------------------------------------
    protected function addSdg(array $sdgStage): void
    {
        foreach ($sdgStage as $k => $v) {
            $this->sdgTotals[$k] += $v;
        }
    }

    protected function getSdgMap(): array
    {
        return [
            'site_selection' => 'sdg11,sdg15',
            'energy_preassessment' => 'sdg13',
            'water_strategy' => 'sdg11',
            'waste_plan' => 'sdg11',
            'sustainability_statement' => 'sdg11',
            'bim_simulation' => 'sdg13',
            'environment_simulations' => 'sdg13',
            'clash_free_bim' => 'sdg11',

            'energy_modelling' => 'sdg7,sdg9',
            'high_eff' => 'sdg3,sdg7,sdg9',
            'passive_design' => 'sdg7,sdg9',
            'rain_harvest' => 'sdg6,sdg9',
            'greywater' => 'sdg6,sdg9',
            'daylight' => 'sdg3,sdg7,sdg9',
            'ventiation' => 'sdg3,sdg7,sdg9',
            'low_carbon' => 'sdg3,sdg7,sdg9',
            'const_waste' => 'sdg3,sdg9',
            'green_land' => 'sdg3,sdg6',
            'heat_strategy' => 'sdg3,sdg7,sdg9',
            'thermal_model' => 'sdg7,sdg9',
            'water_eff' => 'sdg6',
            'mats_lifecycle' => 'sdg3,sdg9',

            'smart_energy' => 'sdg8,sdg12',
            'ctrl_measure' => 'sdg9,sdg12',
            'li_design' => 'sdg9,sdg12',
            'se_ctrl' => 'sdg9,sdg12',
            'avoid' => 'sdg9,sdg12',
            'basic_access' => 'sdg8,sdg9',
            'waste_tracking' => 'sdg9,sdg12',
            'perecentage_recycled' => 'sdg12',
            'my_hijau' => 'sdg9,sdg12',
            'useof_recycle' => 'sdg8,sdg12',
            'recyce_rate' => 'sdg12',
            'reduce_waste' => 'sdg9,sdg12',
            'voc' => 'sdg8,sdg12',
            'led' => 'sdg9,sdg12',
            'motion_sensors' => 'sdg9,sdg12',
            'hvac_systems' => 'sdg9,sdg12',
            'renewable_energy' => 'sdg9,sdg12',
            'energy_management' => 'sdg9,sdg12',
            'energy_star' => 'sdg9',
            'bas_integrated' => 'sdg9',
            'energy_eff' => 'sdg9,sdg12',
            'tolerat_landscape' => 'sdg12',
            'bathroom_pantry' => 'sdg12',
            'water_consumpt' => 'sdg12',
            'water_monitor' => 'sdg9,sdg12',
            'water_leakage' => 'sdg9,sdg12',
            'ventilation_light' => 'sdg8,sdg12',
            'ieq_standard' => 'sdg8,sdg9,sdg12',

            'build_performance' => 'sdg11',
            'air_quaity' => 'sdg3',
            'thermal_comfort' => 'sdg3',
            'cleaning_mats' => 'sdg11',
            'sustain' => 'sdg7',

            'mats_salvage' => 'sdg12',
            'recyclable_percentage' => 'sdg12',
            'waste_segregation' => 'sdg12',
            'audit_docs' => 'sdg8',
            'recycle_analysis' => 'sdg12',
            'eol_impact' => 'sdg13',
        ];
    }

    protected function getMarks(): array
    {
        return [
            'initiation'=>[
                'site_selection'=>2,'energy_preassessment'=>2,'water_strategy'=>2,
                'waste_plan'=>2,'sustainability_statement'=>1,'bim_simulation'=>3,
                'environment_simulations'=>3,'clash_free_bim'=>2
            ],
            'planning'=>[
                'energy_modelling'=>3,'passive_design'=>2,'high_eff'=>2,
                'rain_harvest'=>2,'greywater'=>2,'daylight'=>2,'ventiation'=>2,
                'low_carbon'=>2,'const_waste'=>2,'green_land'=>2,'heat_strategy'=>2,
                'thermal_model'=>3,'water_eff'=>2,'mats_lifecycle'=>3
            ],
            'execution'=>[
                'smart_energy'=>2,'ctrl_measure'=>2,'li_design'=>2,'se_ctrl'=>2,'avoid'=>1,
                'basic_access'=>1,'waste_tracking'=>3,'perecentage_recycled'=>2,'my_hijau'=>2,
                'useof_recycle'=>2,'recyce_rate'=>3,'reduce_waste'=>2,'voc'=>2,'led'=>2,
                'motion_sensors'=>2,'hvac_systems'=>2,'renewable_energy'=>3,'energy_management'=>3,
                'energy_star'=>3,'bas_integrated'=>3,'energy_eff'=>3,'tolerat_landscape'=>1,
                'bathroom_pantry'=>2,'water_eff'=>2,'water_consumpt'=>1,'water_monitor'=>2,
                'water_leakage'=>2,'ventilation_light'=>1,'ieq_standard'=>2
            ],
            'monitoring'=>[
                'smart_energy'=>2,'build_performance'=>2,'air_quaity'=>2,'thermal_comfort'=>2,
                'cleaning_mats'=>2,'sustain'=>2
            ],
            'closing'=>[
                'mats_salvage'=>2,'recyclable_percentage'=>3,'waste_segregation'=>2,
                'audit_docs'=>2,'recycle_analysis'=>3,'eol_impact'=>2
            ]
        ];
    }
}
 