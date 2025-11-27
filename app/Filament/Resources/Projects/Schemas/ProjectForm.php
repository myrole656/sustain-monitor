<?php

namespace App\Filament\Resources\Projects\Schemas;

use Filament\Schemas\Schema;
use Filament\Schemas\Components\Wizard;
use Filament\Schemas\Components\Wizard\Step;
use Filament\Forms\Components\TextInput;
use Filament\Forms\Components\CheckboxList;
use Filament\Forms\Components\DatePicker;
use Filament\Forms\Components\Select;
use Filament\Forms\Components\Hidden;
use Illuminate\Validation\Rule;

class ProjectForm
{
    protected static bool $canCreateAnother = false;
    public static function configure(Schema $schema): Schema
    {
        return $schema->components([
            Wizard::make([
                /* ------------------------------
                |  STEP 1 – PROJECT INFO
                --------------------------------*/
                Step::make('Project Info')
                    ->icon('heroicon-o-clipboard-document')
                    ->schema([
                        TextInput::make('project_name')
                            ->label('Project Name')
                            ->required()
                            ->rules([
                                Rule::unique('projects', 'project_name')
                                    ->where(fn ($query) => $query->where('user_id', auth()->id())),
                            ]),

                        TextInput::make('project_location')
                            ->label('Project Location'),

                        TextInput::make('pic_contact')
                            ->label('PIC Contact'),

                        Select::make('target')
                            ->label('Certification Target')
                            ->options([
                                'PLATINUM'    => 'PLATINUM',
                                'GOLD'        => 'GOLD',
                                'SILVER'      => 'SILVER',
                                'CERTIFICATE' => 'CERTIFICATE',
                            ])
                            ->required(),

                        DatePicker::make('reg_date')
                            ->label('Registration Date'),

                        Hidden::make('user_id')->default(fn () => auth()->id()),
                    ])
                    ->columns(2),

                /* ------------------------------
                |  STEP 2 – INITIATION
                --------------------------------*/
                Step::make('Initiation')
                    ->icon('heroicon-o-rocket-launch')
                    ->schema([
                        CheckboxList::make('initiation_items')
                            ->label('Initiation Checklist')
                            ->options([
                                'site_selection' => 'Site selection complies with low-impact development principles.',
                                'energy_preassessment' => 'Preliminary energy performance pre-assessment completed.',
                                'water_strategy' => 'Initial water-reduction strategy prepared.',
                                'waste_plan' => 'Early waste-minimisation plan drafted.',
                                'sustainability_statement' => 'Sustainability Intent Statement submitted.',
                                'bim_simulation' => 'Initial site environmental simulation using BIM completed.',
                                'environment_simulations' => 'Environmental and site simulations conducted (sun, wind, terrain).',
                                'clash_free_bim' => 'Clash-free planning validated using BIM.',
                            ])
                            ->columns(1)
                            ->required(),
                    ]),

                /* ------------------------------
                |  STEP 3 – PLANNING
                --------------------------------*/
                Step::make('Planning')
                    ->icon('heroicon-o-map')
                    ->columns(2)
                    ->schema([
                        // LEFT COLUMN
                        CheckboxList::make('planning_items_left')
                            ->label('Planning Phase – Part 1')
                            ->options([
                                'energy_modelling' => 'BIM-based energy modelling completed.',
                                'passive_design' => 'Passive design strategies incorporated.',
                                'high_eff' => 'High-efficiency M&E systems selected.',
                                'rain_harvest' => 'Rainwater harvesting integrated.',
                                'greywater' => 'Greywater recycling system designed.',
                                'daylight' => 'Daylighting simulation meets standards.',
                                'ventiation' => 'Natural ventilation strategy incorporated.',
                            ])
                            ->columns(1)
                            ->required(),

                        // RIGHT COLUMN
                        CheckboxList::make('planning_items_right')
                           
                            ->options([
                                'low_carbon' => 'Low-carbon materials specified.',
                                'const_waste' => 'BIM-based quantity optimisation.',
                                'green_land' => 'Green landscape plan prepared.',
                                'heat_strategy' => 'Heat island mitigation included.',
                                'thermal_model' => 'Thermal modelling completed.',
                                'water_eff' => 'Water efficiency simulation done.',
                                'mats_lifecycle' => 'Life-cycle assessment (LCA) conducted.',
                            ])
                            ->columns(1)
                            ->required(),
                    ]),

                /* ------------------------------
                |  STEP 4 – EXECUTION
                --------------------------------*/
                Step::make('Execution')
                    ->icon('heroicon-o-cog-6-tooth')
                    ->schema([
                        CheckboxList::make('process.execution')
                            ->label('Execution Phase Checklist')
                            ->options([
                                'smart_energy' => 'Dust, noise, and pollution monitoring implemented.',
                                'ctrl_measure' => 'Erosion and sedimentation control measures in place.',
                                'li_design' => 'Low-impact design compliance.',
                                'se_ctrl' => 'Green area erosion control.',
                                'avoid' => 'Flood-zone avoidance reviewed.',
                                'basic_access' => 'Access to public transport ensured.',

                                'waste_tracking' => 'Real-time waste tracking.',
                                'perecentage_recycled' => 'Recycled materials tracked.',
                                'my_hijau' => 'Green-certified materials used.',
                                'useof_recycle' => 'Recycled/local materials used.',
                                'recyce_rate' => 'Achieved 50% recycling.',
                                'reduce_waste' => 'Prefabricated components used.',
                                'voc' => 'Low-VOC materials used.',

                                'led' => 'LED lighting installed.',
                                'motion_sensors' => 'Motion sensors installed.',
                                'hvac_systems' => 'Efficient HVAC installed.',
                                'renewable_energy' => 'Renewables installed.',
                                'energy_management' => 'Smart energy management system.',
                                'energy_star' => 'MS 1525 compliance.',
                                'bas_integrated' => 'BAS integrated.',
                                'energy_eff' => 'Efficient building envelope.',

                                'tolerat_landscape' => 'Drought-tolerant landscaping.',
                                'bathroom_pantry' => 'Low-flow fixtures installed.',
                                'water_eff' => 'Water-efficient systems.',
                                'water_consumpt' => 'Water reduction achieved.',
                                'water_monitor' => 'Water monitoring installed.',
                                'water_leakage' => 'Leak detection system.',

                                'ventilation_light' => 'Good ventilation & lighting.',
                                'ieq_standard' => 'IEQ standards achieved.',
                            ])
                            ->columns(3)
                            ->required(),
                    ]),

                /* ------------------------------
                |  STEP 5 – MONITORING
                --------------------------------*/
                Step::make('Monitoring')
                    ->icon('heroicon-o-chart-bar')
                    ->schema([
                        CheckboxList::make('process.monitoring')
                            ->label('Monitoring Phase')
                            ->options([
                                'smart_energy' => 'Smart energy meters.',
                                'build_performance' => 'Monthly reviews.',
                                'air_quaity' => 'IAQ sensors monitored.',
                                'thermal_comfort' => 'Thermal comfort maintained.',
                                'cleaning_mats' => 'Green cleaning materials.',
                                'sustain' => 'Sustainability-aligned maintenance.',
                            ])
                            ->columns(1)
                            ->required(),
                    ]),

                /* ------------------------------
                |  STEP 6 – CLOSING
                --------------------------------*/
                Step::make('Closing')
                    ->icon('heroicon-o-lock-closed')
                    ->schema([
                        CheckboxList::make('process.closing')
                            ->label('Closing Phase Checklist')
                            ->options([
                                'mats_salvage' => 'Material salvage inventory.',
                                'recyclable_percentage' => 'Recyclable material requirements met.',
                                'waste_segregation' => 'Waste segregation implemented.',
                                'audit_docs' => 'Audit documents prepared.',
                                'recycle_analysis' => 'Recycling performance analysed.',
                                'eol_impact' => 'End-of-life impact simulation.',
                            ])
                            ->columns(1)
                            ->required(),
                    ]),

            ])
            ->columnSpanFull()
            ->skippable(), // Wizard default submit button will be displayed automatically


        ]);
    }
}
