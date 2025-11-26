<?php

namespace App\Filament\Resources\Projects\Pages;

use Filament\Forms\Components\Actions\Action;
use App\Filament\Resources\Projects\ProjectResource;
use Filament\Resources\Pages\CreateRecord;
use App\Models\Process;
use App\Models\Project;
use Filament\Exceptions\FormsValidationException;

class CreateProject extends CreateRecord
{
    protected static string $resource = ProjectResource::class;
    protected static bool $canCreateAnother = false;

    protected array $processData = [];

  


    protected function mutateFormDataBeforeCreate(array $data): array
    {
        // Prevent duplicate project name for this user
        if (Project::where('user_id', auth()->id())
            ->where('project_name', $data['project_name'])
            ->exists()) {
            throw FormsValidationException::withMessages([
                'project_name' => 'You already have a project with this name.  Please make a new one or edit existing Projct',
            ]);
        }

        $this->processData = $data['process'] ?? [];

        // STEP 2 – Initiation
        if (!empty($data['initiation_items'])) {
            $marks = [
                'site_selection' => 2,
                'energy_preassessment' => 2,
                'water_strategy' => 2,
                'waste_plan' => 2,
                'sustainability_statement' => 1,
                'bim_simulation' => 3,
                'environment_simulations' => 3,
                'clash_free_bim' => 2,
            ];
            $this->processData['initiation'] =
                array_sum(array_map(fn($i) => $marks[$i] ?? 0, $data['initiation_items']));
        }

        // STEP 3 – Planning
        if (!empty($data['planning_items_left']) || !empty($data['planning_items_right'])) {
            $marks = [
                'energy_modelling' => 3,
                'passive_design' => 2,
                'high_eff' => 2,
                'rain_harvest' => 2,
                'greywater' => 2,
                'daylight' => 2,
                'ventiation' => 2,
                'low_carbon' => 2,
                'const_waste' => 2,
                'green_land' => 2,
                'heat_strategy' => 2,
                'thermal_model' => 3,
                'water_eff' => 2,
                'mats_lifecycle' => 3,
            ];
            $items = array_merge($data['planning_items_left'] ?? [], $data['planning_items_right'] ?? []);
            $this->processData['planning'] = array_sum(array_map(fn($i) => $marks[$i] ?? 0, $items));
        }

        // STEP 4 – Execution
        if (!empty($this->processData['execution'])) {
            $marks = [
                'smart_energy' => 2, 'ctrl_measure' => 2, 'li_design' => 2,
                'se_ctrl' => 2, 'avoid' => 1, 'basic_access' => 1,
                'waste_tracking' => 3, 'perecentage_recycled' => 2, 'my_hijau' => 2,
                'useof_recycle' => 2, 'recyce_rate' => 3, 'reduce_waste' => 2, 'voc' => 2,
                'led' => 2, 'motion_sensors' => 2, 'hvac_systems' => 2, 'renewable_energy' => 3,
                'energy_management' => 3, 'energy_star' => 3, 'bas_integrated' => 3, 'energy_eff' => 3,
                'tolerat_landscape' => 1, 'bathroom_pantry' => 2, 'water_eff' => 2, 'water_consumpt' => 1,
                'water_monitor' => 2, 'water_leakage' => 2, 'ventilation_light' => 1, 'ieq_standard' => 2,
            ];
            $this->processData['execution'] =
                array_sum(array_map(fn($i) => $marks[$i] ?? 0, $this->processData['execution']));
        }

        // STEP 5 – Monitoring
        if (!empty($this->processData['monitoring'])) {
            $marks = [
                'smart_energy' => 2, 'build_performance' => 2, 'air_quaity' => 2,
                'thermal_comfort' => 2, 'cleaning_mats' => 2, 'sustain' => 2,
            ];
            $this->processData['monitoring'] =
                array_sum(array_map(fn($i) => $marks[$i] ?? 0, $this->processData['monitoring']));
        }

        // STEP 6 – Closing
        if (!empty($this->processData['closing'])) {
            $marks = [
                'mats_salvage' => 2, 'recyclable_percentage' => 3,
                'waste_segregation' => 2, 'audit_docs' => 2, 'recycle_analysis' => 3,
                'eol_impact' => 2,
            ];
            $this->processData['closing'] =
                array_sum(array_map(fn($i) => $marks[$i] ?? 0, $this->processData['closing']));
        }

        unset($data['process'], $data['initiation_items'], $data['planning_items_left'], $data['planning_items_right']);

        return $data;
    }

    protected function afterCreate(): void
    {
        if (empty($this->processData)) return;

        $score = ($this->processData['initiation'] ?? 0)
               + ($this->processData['planning'] ?? 0)
               + ($this->processData['execution'] ?? 0)
               + ($this->processData['monitoring'] ?? 0)
               + ($this->processData['closing'] ?? 0);

        $status = match (true) {
            $score >= 86 => 'PLATINUM',
            $score >= 76 => 'GOLD',
            $score >= 66 => 'SILVER',
            $score >= 50 => 'CERTIFIED',
            default => 'FAIL',
        };

        Process::create([
            'project_id' => $this->record->id,
            'initiation' => $this->processData['initiation'] ?? 0,
            'planning'   => $this->processData['planning'] ?? 0,
            'execution'  => $this->processData['execution'] ?? 0,
            'monitoring' => $this->processData['monitoring'] ?? 0,
            'closing'    => $this->processData['closing'] ?? 0,
            'status'     => $status,
        ]);
    }
}
